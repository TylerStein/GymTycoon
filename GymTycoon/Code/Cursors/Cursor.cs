using GymTycoon.Code.AI;
using GymTycoon.Code.Common;
using GymTycoon.Code.Data;
using ImGuiNET;
using System;

namespace GymTycoon.Code.Cursors
{
    public interface ICursor
    {
        public void Update();
        public void Initialize();
        public void Destroy();
        public void DrawImGui();
    }

    public class Cursor
    {
        private ICursor _activeCursor;
        public ICursor GetActiveCursor;

        public Cursor()
        {
            _activeCursor = new SelectCursor();
        }

        public bool ActiveCursorIs(Type type)
        {
            return _activeCursor.GetType().IsAssignableFrom(type)
                || _activeCursor.GetType().IsSubclassOf(type);
        }

        public void Update()
        {
            _activeCursor.Update();
        }

        public void SetCursor(ICursor cursor)
        {
            if (_activeCursor != null)
            {
                _activeCursor.Destroy();
            }

            _activeCursor = cursor;
            _activeCursor.Initialize();
        }

        public bool TryGetCursor<T>(out T cursor) where T : ICursor
        {
            if (ActiveCursorIs(typeof(T)))
            {
                cursor = (T)_activeCursor;
                return true;
            }

            cursor = default;
            return false;
        }

        public bool GetSelectedGuest(out Guest guest)
        {
            SelectCursor cursor;
            if (TryGetCursor(out cursor))
            {
                Hitbox hitbox;
                if (cursor.GetCurrentSelectedHitbox(out hitbox))
                {
                    guest = hitbox.guest;
                    return guest != null;
                }
            }

            guest = null;
            return false;
        }
        public bool GetSelectedDynamicObjectInstance(out DynamicObjectInstance inst)
        {
            SelectCursor cursor;
            if (TryGetCursor(out cursor))
            {
                Hitbox hitbox;
                if (cursor.GetCurrentSelectedHitbox(out hitbox))
                {
                    inst = hitbox.dynamicObjectInstance;
                    return inst != null;
                }
            }

            inst = null;
            return false;
        }

        //public void SetSelectedObject(DynamicObjectInstance obj)
        //{
        //    if (_activeType != CursorType.Select)
        //    {
        //        SetCursor(CursorType.Select);
        //    }

        //    (_activeCursor as SelectCursor).SetSelection(obj);
        //}

        //public void SetPlacementObject(DynamicObject objType)
        //{
        //    if (_activeType != CursorType.Place)
        //    {
        //        SetCursor(CursorType.Place);
        //    }

        //    (_activeCursor as PlaceCursor).SetPlacement(objType);
        //}

        //public bool TryGetPlacementDrawData(out SpriteInstance spriteInstance, out Direction dir)
        //{
        //    if (_activeType != CursorType.Place)
        //    {
        //        spriteInstance = null;
        //        dir = default;
        //        return false;
        //    }

        //    PlaceCursor place = (PlaceCursor)_activeCursor;
        //    spriteInstance = place.PlaceObjectSpriteInstance;
        //    dir = place.PlaceDirection;
        //    return place.PlaceObjectType != null;
        //}

        //public bool IsSelectedObject(DynamicObjectInstance inst)
        //{
        //    if (_activeType != CursorType.Select)
        //    {
        //        return false;
        //    }

        //    return (_activeCursor as SelectCursor).IsSelected(inst);
        //}

        //public Hitbox[] GetHitboxes()
        //{
        //    if (_activeType != CursorType.Select)
        //    {
        //        return new Hitbox[0];
        //    }

        //    return (_activeCursor as SelectCursor).GetHitboxes();
        //}

        public void DrawImGui()
        {
            ImGui.Begin("Cursor");
            if (ImGui.Button("Diselect"))
            {
                SetCursor(new SelectCursor());
            }
            ImGui.Separator();
            _activeCursor.DrawImGui();
            ImGui.End();
        }
    }
}
