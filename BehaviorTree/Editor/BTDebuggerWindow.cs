﻿using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Saro.BT
{
    public class BTDebuggerWindow : EditorWindow
    {
        private const int nestedPadding = 14;

        public static Transform selectedObject;
        public static UBTDebugger selectedDebugger;

        private Vector2 scrollPosition = Vector2.zero;

        private GUIStyle smallTextStyle, nodeCapsuleGray, nodeCapsuleFailed;
        private GUIStyle nestedBoxStyle;

        private Color defaultColor;

        [MenuItem("Tools/BT Debugger")]
        public static void ShowWindow()
        {
            BTDebuggerWindow window = EditorWindow.GetWindow<BTDebuggerWindow>(false, "BT Debugger");
            window.Show();
        }

        public void Init()
        {
            //Debug.Log("Init !!");

            nestedBoxStyle = new GUIStyle();
            nestedBoxStyle.margin = new RectOffset(nestedPadding, 0, 0, 0);

            smallTextStyle = new GUIStyle();
            smallTextStyle.font = EditorStyles.miniFont;
            smallTextStyle.richText = true;

            nodeCapsuleGray = (GUIStyle)"helpbox";
            nodeCapsuleGray.richText = true;
            nodeCapsuleGray.normal.textColor = Color.black;

            nodeCapsuleFailed = new GUIStyle(nodeCapsuleGray);
            nodeCapsuleFailed.richText = true;
            nodeCapsuleFailed.normal.textColor = Color.red;

            defaultColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
        }

        public void OnSelectionChange()
        {
            selectedObject = Selection.activeTransform;
            if (selectedObject != null) selectedDebugger = selectedObject.GetComponentInChildren<UBTDebugger>();

            Repaint();
        }

        public void OnGUI()
        {
            Init();

            GUI.color = defaultColor;
            GUILayout.Toggle(false, "BT Debugger", GUI.skin.FindStyle("LODLevelNotifyText"));
            GUI.color = Color.white;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Cannot use this utility in Editor Mode", MessageType.Info);
                return;
            }

            var newDebugger = (UBTDebugger)EditorGUILayout.ObjectField("Selected Debugger:", selectedDebugger, typeof(UBTDebugger), true);

            if (newDebugger != selectedDebugger)
            {
                selectedDebugger = newDebugger;
                if (newDebugger != null) selectedObject = selectedDebugger.transform;
            }

            if (selectedObject == null)
            {
                EditorGUILayout.HelpBox("Please select an object", MessageType.Info);
                return;
            }

            if (selectedDebugger == null)
            {
                EditorGUILayout.HelpBox("This object does not contain a debugger component", MessageType.Info);
                return;
            }
            else if (selectedDebugger.behaviorTree == null)
            {
                EditorGUILayout.HelpBox("BehavorTree is null", MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.BeginHorizontal();
            DrawBlackboardKeyAndValues("Blackboard:", selectedDebugger.behaviorTree.Blackboard);
            if (selectedDebugger.CustomStats.Keys.Count > 0)
            {
                DrawBlackboardKeyAndValues("Custom Stats:", selectedDebugger.CustomStats);
            }
            DrawStats(selectedDebugger);
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            if (Time.timeScale <= 2.0f)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("TimeScale: ");
                Time.timeScale = EditorGUILayout.Slider(Time.timeScale, 0.0f, 2.0f);
                GUILayout.EndHorizontal();
            }

            DrawBehaviourTree(selectedDebugger);
            GUILayout.Space(10);

            EditorGUILayout.EndScrollView();

            Repaint();
        }

        private void DrawStats(UBTDebugger debugger)
        {
            EditorGUILayout.BeginVertical();
            {
                GUILayout.Label("Stats:", EditorStyles.boldLabel);

                Root behaviorTree = debugger.behaviorTree;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    DrawKeyValue("Active Timers:  ", behaviorTree.Clock.NumTimers.ToString());
                    DrawKeyValue("Timer Pool Size:  ", behaviorTree.Clock.DebugPoolSize.ToString());
                    DrawKeyValue("Active Update Observers:  ", behaviorTree.Clock.NumUpdateObservers.ToString());
                    DrawKeyValue("Active Blackboard Observers:  ", behaviorTree.Blackboard.NumObservers.ToString());
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawBlackboardKeyAndValues(string label, Blackboard blackboard)
        {
            EditorGUILayout.BeginVertical();
            {
                GUILayout.Label(label, EditorStyles.boldLabel);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    List<string> keys = blackboard.Keys;
                    foreach (string key in keys)
                    {
                        DrawKeyValue(key, blackboard.Get(key).ToString());
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawKeyValue(string key, string value)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(key, smallTextStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label(value, smallTextStyle);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBehaviourTree(UBTDebugger debugger)
        {
            EditorGUILayout.BeginVertical();
            {
                GUILayout.Label("Behaviour Tree:", EditorStyles.boldLabel);

                EditorGUILayout.BeginVertical(nestedBoxStyle);
                DrawNodeTree(debugger.behaviorTree, 0);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawNodeTree(Node node, int depth = 0, bool firstNode = true, float lastYPos = 0f)
        {
            bool decorator = node is Decorator && !(node is Root);
            bool parentIsDecorator = (node.ParentContainerNode is Decorator);
            GUI.color = (node.CurrentState == Node.State.ACTIVE) ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 1f, 1f, 0.3f);

            if (!parentIsDecorator)
            {
                DrawSpacing();
            }

            bool drawConnected = !decorator || (decorator && ((Container)node).Collapse);
            DrawNode(node, depth, drawConnected);

            Rect rect = GUILayoutUtility.GetLastRect();

            // Set intial line position
            if (firstNode)
            {
                lastYPos = rect.yMin;
            }

            // Draw the lines
            Handles.BeginGUI();

            // Container collapsing
            Container container = node as Container;
            Rect interactionRect = new Rect(rect);
            interactionRect.width = 100;
            interactionRect.y += 8;
            if (container != null && Event.current.type == EventType.MouseUp && Event.current.button == 0 && interactionRect.Contains(Event.current.mousePosition))
            {
                container.Collapse = !container.Collapse;
                Event.current.Use();
            }

            Handles.color = new Color(0f, 0f, 0f, 1f);
            if (!decorator)
            {
                Handles.DrawLine(new Vector2(rect.xMin - 5, lastYPos), new Vector2(rect.xMin - 5, rect.yMax - 4));
            }
            else
            {
                Handles.DrawLine(new Vector2(rect.xMin - 5, lastYPos - 6), new Vector2(rect.xMin - 5, rect.yMax + 6));
            }
            Handles.EndGUI();

            if (decorator) depth++;

            if (node is Container && !((Container)node).Collapse)
            {
                if (!decorator) EditorGUILayout.BeginVertical(nestedBoxStyle);

                Node[] children = (node as Container).DebugChildren;
                if (children == null)
                {
                    GUILayout.Label("CHILDREN ARE NULL");
                }
                else
                {
                    lastYPos = rect.yMin + 16; // Set new Line position

                    for (int i = 0; i < children.Length; i++)
                    {
                        DrawNodeTree(children[i], depth, i == 0, lastYPos);
                    }
                }

                if (!decorator) EditorGUILayout.EndVertical();
            }

        }

        private void DrawSpacing()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawNode(Node node, int depth, bool connected)
        {
            bool inactive = node.CurrentState != Node.State.ACTIVE;
            float alpha = inactive ? .4f : 1f;
            bool failed = !node.debugLastResult && inactive;

            EditorGUILayout.BeginHorizontal();
            {
                GUI.color = new Color(1f, 1f, 1f, alpha);

                string tagName;
                //GUIStyle tagStyle = stopRequested ? nodeCapsuleStopRequested : (failed ? nodeCapsuleFailed : nodeCapsuleGray);
                GUIStyle tagStyle = failed ? nodeCapsuleFailed : nodeCapsuleGray;
                tagStyle.richText = true;
                bool drawLabel = !string.IsNullOrEmpty(node.Label);
                string label = node.Label;

                if (node is BlackboardCondition)
                {
                    BlackboardCondition nodeBlackboardCond = node as BlackboardCondition;
                    tagName = nodeBlackboardCond.ToString();
                    //tagName = nodeBlackboardCond.Key + " " + operatorToString[nodeBlackboardCond.Operator] + " " + nodeBlackboardCond.Value;
                    GUI.backgroundColor = new Color(0.9f, 0.9f, 0.6f);
                }
                else
                {
                    if (node is Composite) GUI.backgroundColor = new Color(0.3f, 1f, 0.1f);
                    if (node is Decorator) GUI.backgroundColor = new Color(0.3f, 1f, 1f);
                    if (node is Task) GUI.backgroundColor = new Color(0.5f, 0.1f, 0.5f);
                    if (node is ObservingDecorator) GUI.backgroundColor = new Color(0.9f, 0.9f, 0.6f);

                    //nameToTagString.TryGetValue(node.Name, out tagName);
                    tagName = node.ToString();
                }

                //if (string.IsNullOrEmpty(tagName)) tagName = node.ToString();

                if ((node is Container) && ((Container)node).Collapse)
                {
                    if (!drawLabel)
                    {
                        drawLabel = true;
                        label = tagName;
                    }
                    tagName = "...";
                    GUI.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
                }



                if (!drawLabel)
                {
                    GUILayout.Label(tagName, tagStyle);
                }
                else
                {
                    GUILayout.Label(string.Format("{0} {1} ", tagName, label), tagStyle);
                    // Reset background color
                    GUI.backgroundColor = Color.white;
                }

                // Draw Stats
                if (node is Task task)
                {
                    switch (task.ResultType)
                    {
                        case Task.Result.SUCCESS:
                            GUILayout.Label("<color=green>✔</color>", tagStyle);
                            break;
                        case Task.Result.FAILED:
                            GUILayout.Label("<color=red>✘</color>", tagStyle);
                            break;
                        case Task.Result.BLOCKED:
                            GUILayout.Label("<color=blue><b><i>N</i></b></color>", tagStyle);
                            break;
                        case Task.Result.PROGRESS:
                            GUILayout.Label("<color=yellow><b><i>O</i></b></color>", tagStyle);
                            break;
                        default:
                            break;
                    }

                }
                else
                {
                    GUILayout.Label(node.debugLastResult ? "<color=green>✔</color>" : "<color=red>✘</color>", tagStyle);
                }


                GUILayout.FlexibleSpace();

                // Draw Buttons
                if (node.CurrentState == Node.State.ACTIVE)
                {
                    if (GUILayout.Button("cancel", tagStyle))
                    {
                        node.Cancel();
                    }
                }
                else if (node is Root)
                {
                    GUI.color = new Color(1f, 1f, 1f, 1f);
                    if (GUILayout.Button("start", tagStyle))
                    {
                        node.Start();
                    }
                    GUI.color = new Color(1f, 1f, 1f, 0.3f);
                }

            }

            EditorGUILayout.EndHorizontal();

            // Draw the lines
            if (connected)
            {
                Rect rect = GUILayoutUtility.GetLastRect();

                Handles.color = new Color(0f, 0f, 0f, 1f);
                Handles.BeginGUI();
                float midY = 4 + (rect.yMin + rect.yMax) / 2f;
                Handles.DrawLine(new Vector2(rect.xMin - 5, midY), new Vector2(rect.xMin, midY));
                Handles.EndGUI();
            }


        }

    }
}