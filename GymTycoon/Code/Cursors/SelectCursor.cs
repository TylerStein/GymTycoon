using Microsoft.Xna.Framework;
using GymTycoon.Code.Common;
using ImGuiNET;
using System.Collections.Generic;
using GymTycoon.Code.AI;
using GymTycoon.Code.Data;

namespace GymTycoon.Code.Cursors
{
    public class Hitbox
    {
        public Rectangle Rect;
        public float Depth;
        public DynamicObjectInstance dynamicObjectInstance;
        public Guest guest;
    }

    public class SelectCursor : ICursor
    {
        const int MaxSelectOptions = 8; // max num hovered over hitboxes to consider

        private List<Hitbox> _hitboxes = []; // all hitboxes on the screen
        private Hitbox[] _selectOptions = new Hitbox[MaxSelectOptions]; // hitboxes hovered over
        private int _currentSelectOptions = 0; // number of hitboxes hovered over
        private int _currentSelection = -1; // active option out of current hovered hitboxes

        private Hitbox _selectedHitbox; // actual "selected" hitbox

        public SelectCursor(DynamicObjectInstance selectedObject)
        {
            _selectedHitbox = new Hitbox()
            {
                dynamicObjectInstance = selectedObject
            };
        }

        public SelectCursor(Guest selectedGuest)
        {
            _selectedHitbox = new Hitbox()
            {
                guest = selectedGuest,
            };
        }

        public SelectCursor()
        {
            _selectedHitbox = null;
        }

        public void Update()
        {
            _hitboxes.Clear();
            BuildHitboxes(
                GameInstance.Instance.World,
                GameInstance.Instance.Director,
                GameInstance.Instance.WorldRenderer
            );
            UpdateCurrentSelection(GameInstance.Instance.Input.MousePosition);

            if (GameInstance.Instance.Input.GetBinaryAction(GameInstance.SymbolInputRotate).Pressed)
            {
                Rotate();
            }

            if (GameInstance.Instance.Input.MouseIsOnScreen && GameInstance.Instance.Input.GetBinaryAction(GameInstance.SymbolInputSelect).Pressed)
            {
                Hitbox lastSelected = _selectedHitbox;

                Hitbox selection;
                if (GetCurrentHoverHitbox(out selection))
                {
                    if (_selectedHitbox == selection)
                    {
                        // same hitbox probably
                        NextSelection();
                    }

                    _selectedHitbox = selection;
                }
                else
                {
                    _selectedHitbox = null;
                }

                if (_selectedHitbox != lastSelected && lastSelected != null)
                {
                    // TODO: Better handle unselect behavior
                    if (lastSelected.guest != null)
                    {
                        lastSelected.guest.FollowCam = false;
                    }

                    if (lastSelected.dynamicObjectInstance != null)
                    {
                        // Stuff
                    }
                }
            }

            if (GameInstance.Instance.Input.GetBinaryAction(GameInstance.SymbolInputDelete).Pressed)
            {
                Delete();
            }
        }

        public void Rotate()
        {
            if (_selectedHitbox != null && _selectedHitbox.dynamicObjectInstance != null)
            {
                _selectedHitbox.dynamicObjectInstance.Rotate();
            }
        }

        public void Delete()
        {
            if (_selectedHitbox != null && _selectedHitbox.dynamicObjectInstance != null)
            {
                DynamicObjectCategory category = _selectedHitbox.dynamicObjectInstance.Data.Category;
                GameInstance.Instance.World.DeleteDynamicObject(_selectedHitbox.dynamicObjectInstance);
                GameInstance.Instance.UpdateZoneForObjectType(category);
            }
        }

        public void SetSelection(DynamicObjectInstance obj)
        {
            _selectedHitbox = new Hitbox()
            {
                dynamicObjectInstance = obj,
            };
        }
        public void SetSelection(Guest guest)
        {
            _selectedHitbox = new Hitbox()
            {
                guest = guest,
            };
        }

        public int GetCurrentSelection()
        {
            return _currentSelection;
        }

        public bool GetCurrentHoverHitbox(out Hitbox hitbox)
        {
            if (_currentSelection == -1)
            {
                hitbox = default;
                return false;
            }

            hitbox = _selectOptions[_currentSelection];
            return true;
        }
        
        public bool GetCurrentSelectedHitbox(out Hitbox hitbox)
        {
            hitbox = _selectedHitbox;
            return hitbox != null;
        }

        public void NextSelection()
        {
            if (_selectOptions.Length == 0)
            {
                return;
            }

            _currentSelection++;
            if (_currentSelection == _currentSelectOptions)
            {
                _currentSelection = 0;
            }
        }
        public void PrevSelection()
        {
            if (_selectOptions.Length == 0)
            {
                return;
            }

            _currentSelection--;
            if (_currentSelection == 0)
            {
                _currentSelection = _currentSelectOptions;
            }
        }

        public void BuildHitboxes(World world, Director director, WorldRenderer renderer)
        {
            Point3 posMin;
            Point screenMin, size;
            Point halfRenderSize = new Point(renderer.GetDrawTileSize() / 2, renderer.GetDrawTileSize() / 2);

            foreach (var obj in world.GetAllDynamicObjects())
            {
                posMin = world.GetPosition(obj.WorldPosition);
                screenMin = renderer.WorldToScreen(posMin) - halfRenderSize;
                size = new Point(obj.Data.Width * renderer.GetDrawTileSize(), obj.Data.Height * renderer.GetDrawTileSize());
                _hitboxes.Add(new Hitbox()
                {
                    Rect = new Rectangle(screenMin.X, screenMin.Y, size.X, size.Y),
                    Depth = WorldRenderer.GetDepth(posMin, world.GetWidth(), world.GetHeight()),
                    dynamicObjectInstance = obj.Parent != null ? obj.Parent : obj
                });
            }

            foreach (var guest in director.ActiveGuests)
            {
                posMin = world.GetPosition(guest.WorldPosition);
                screenMin = renderer.WorldToScreen(posMin) - halfRenderSize;
                size = new Point(renderer.GetDrawTileSize(), renderer.GetDrawTileSize());
                _hitboxes.Add(new Hitbox()
                {
                    Rect = new Rectangle(screenMin.X, screenMin.Y, size.X, size.Y),
                    Depth = WorldRenderer.GetDepth(posMin, world.GetWidth(), world.GetHeight()),
                    guest = guest,
                });
            }
        }

        public int GetHitboxesUnderCursor(Point cursor, Hitbox[] output, int max)
        {
            int found = 0;
            for (int i = 0; i < _hitboxes.Count && found < max; i++)
            {
                if (_hitboxes[i].Rect.Contains(cursor))
                {
                    output[found] = _hitboxes[i];
                    found++;
                }
            }

            return found;
        }

        public void UpdateCurrentSelection(Point cursor)
        {
            _currentSelectOptions = GetHitboxesUnderCursor(cursor, _selectOptions, MaxSelectOptions);
            if (_currentSelectOptions > 0)
            {
                _currentSelection = 0;
            }
            else
            {
                _currentSelection = -1;
            }
        }

        public List<Hitbox> GetHitboxes()
        {
            return _hitboxes;
        }

        public bool IsSelected(DynamicObjectInstance inst)
        {
            if (_selectedHitbox == null)
            {
                return false;
            }

            return 
                _selectedHitbox.dynamicObjectInstance == inst
                || (inst.Parent != null && inst.Parent == _selectedHitbox.dynamicObjectInstance)
                || (inst.Children.Contains(_selectedHitbox.dynamicObjectInstance));
        }

        public bool IsSelected(Guest guest)
        {
            return _selectedHitbox != null && _selectedHitbox.guest == guest;
        }

        public void DrawImGui()
        {
            if (_selectedHitbox != null)
            {
                ImGui.Text("Selected Object");
                if (_selectedHitbox.dynamicObjectInstance != null)
                {
                    DynamicObjectInstance inst = _selectedHitbox.dynamicObjectInstance;
                    ImGui.Text($"Name: {inst.Data.GetFullName()}");
                    ImGui.Text($"Position: {inst.WorldPosition}");
                    ImGui.Text($"ID: {inst.Id}");
                    if (inst.Parent != null)
                    {
                        ImGui.Text($"Parent ID: {inst.Parent.Id}");
                    }

                    if (ImGui.SmallButton("Delete"))
                    {
                        Delete();
                    }
                    if (ImGui.SmallButton("Rotate"))
                    {
                        inst.Rotate();
                    }
                }
                else if (_selectedHitbox.guest != null)
                {
                    Guest guest = _selectedHitbox.guest;
                    ImGui.Text($"Name: {guest}");
                    ImGui.Text($"Position: {guest.WorldPosition}");
                }

                ImGui.Separator();
            }
        }

        public void Initialize()
        {
            _currentSelection = -1;
            _selectedHitbox = null;
        }

        public void Destroy()
        {
            _currentSelection = -1;
            _selectedHitbox = null;
        }
    }
}
