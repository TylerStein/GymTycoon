using ImGuiNET;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GymTycoon.Code
{
    public class Performance
    {
        private class PerformanceNode
        {
            public string Label { get; }
            public long TotalTime { get; private set; }
            public int CallCount { get; private set; }
            public float CallsPerSecond { get; private set; }
            public List<PerformanceNode> Children { get; }

            private Stopwatch _stopwatch;

            public PerformanceNode(string label)
            {
                Label = label;
                Children = new List<PerformanceNode>();
                _stopwatch = new Stopwatch();
            }

            public void Start()
            {
                _stopwatch.Restart();
            }

            public void Stop()
            {
                _stopwatch.Stop();
                TotalTime += _stopwatch.ElapsedMilliseconds;
                CallCount++;
            }

            public void UpdateCallsPerSecond()
            {
                CallsPerSecond = CallCount;
                CallCount = 0;
                foreach (var child in Children)
                {
                    child.UpdateCallsPerSecond();
                }
            }

            public void DrawImGui()
            {
                if (ImGui.TreeNode(Label, $"{Label} ({TotalTime} ms, {CallsPerSecond} cps)"))
                {
                    foreach (var child in Children.OrderByDescending(c => c.TotalTime))
                    {
                        child.DrawImGui();
                    }
                    ImGui.TreePop();
                }
            }
        }

        private PerformanceNode _rootNode;
        private Stack<PerformanceNode> _nodeStack;

        // Fields for tracking FPS
        private int _frameCount = 0;
        private float _elapsedTime = 0f;
        private float _fps = 0f;

        public Performance()
        {
            _rootNode = new PerformanceNode("Root");
            _nodeStack = new Stack<PerformanceNode>();
            _nodeStack.Push(_rootNode);
        }

        public void Start(string label)
        {
            var currentNode = _nodeStack.Peek();
            var childNode = currentNode.Children.FirstOrDefault(c => c.Label == label);
            if (childNode == null)
            {
                childNode = new PerformanceNode(label);
                currentNode.Children.Add(childNode);
            }
            childNode.Start();
            _nodeStack.Push(childNode);
        }

        public void Stop()
        {
            var currentNode = _nodeStack.Pop();
            currentNode.Stop();
        }

        public void UpdateFPS(GameTime gameTime)
        {
            // Update FPS
            _frameCount++;
            _elapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_elapsedTime >= 1.0f)
            {
                _fps = _frameCount / _elapsedTime;
                _frameCount = 0;
                _elapsedTime = 0f;

                _rootNode.UpdateCallsPerSecond();
            }
        }

        public void DrawImGui()
        {
            // Display FPS
            ImGui.Begin("Performance");
            ImGui.Text($"FPS: {_fps:F2}");
            _rootNode.DrawImGui();
            ImGui.End();
        }
    }
}