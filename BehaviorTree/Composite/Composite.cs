﻿
using UnityEngine.Assertions;

namespace Saro.BT
{
    public abstract class Composite : Container
    {
        protected Node[] m_children;

        public virtual bool CanAbortSelf => true;
        public virtual bool CanAbortLowerPriority => true;

        public Composite(string name, params Node[] children) : base(name)
        {
            m_children = children;
            Assert.IsTrue(children.Length > 0, "Composite nodes (Selector, Sequence, Parallel) need at least one child!");
            foreach (var node in m_children)
            {
                node.SetParent(this);
                (node as ObservingDecorator)?.UpdateAbortsType();
            }
        }

        protected override void Stopped(bool success)
        {
            foreach (Node child in m_children)
            {
                child.ParentCompositeStopped(this);
            }
            base.Stopped(success);
        }

        public override void SetRoot(Root rootNode)
        {
            base.SetRoot(rootNode);

            foreach (Node node in m_children)
            {
                node.SetRoot(rootNode);
            }
        }

        public abstract void AbortTreeNode(Node child);

        [System.Obsolete("obsolete")]
        public abstract void StopLowerPriorityChildrenForChild(Node child, bool immediateRestart);

#if UNITY_EDITOR
        public override Node[] DebugChildren { get => m_children; }

        public Node DebugGetActiveChild()
        {
            foreach (Node node in DebugChildren)
            {
                if (node.CurrentState == Node.State.ACTIVE)
                {
                    return node;
                }
            }

            return null;
        }
#endif
    }
}