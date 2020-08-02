using LegendaryTools.Inspector;
using UnityEngine;

namespace LegendaryTools.UI
{
    /// <summary>
    /// Property binding lets you bind two fields or properties so that changing one will update the other.
    /// </summary>
    [ExecuteInEditMode, AddComponentMenu("UI/Binding/Property Sync")]
    public class PropertySync : MonoBehaviour
    {
        public enum Direction
        {
            SourceUpdatesTarget,
            TargetUpdatesSource,
            BiDirectional
        }

        public enum UpdateCondition
        {
            OnStart,
            OnUpdate,
            OnLateUpdate,
            OnFixedUpdate
        }

        /// <summary>
        /// Direction of updates.
        /// </summary>
        public Direction direction = Direction.SourceUpdatesTarget;

        /// <summary>
        /// Whether the values will update while in edit mode.
        /// </summary>
        public bool editMode = true;

        public bool invertBool;

        // Cached value from the last update, used to see which property changes for bi-directional updates.
        private object mLastValue;

        /// <summary>
        /// First property reference.
        /// </summary>
        public PropertyBindingReference source;

        /// <summary>
        /// Second property reference.
        /// </summary>
        public PropertyBindingReference target;

        /// <summary>
        /// When the property update will occur.
        /// </summary>
        public UpdateCondition update = UpdateCondition.OnUpdate;

        private void Start()
        {
            UpdateTarget();
            if (update == UpdateCondition.OnStart)
            {
                enabled = false;
            }
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (!editMode && !Application.isPlaying)
            {
                return;
            }
#endif
            if (update == UpdateCondition.OnUpdate)
            {
                UpdateTarget();
            }
        }

        private void LateUpdate()
        {
#if UNITY_EDITOR
            if (!editMode && !Application.isPlaying)
            {
                return;
            }
#endif
            if (update == UpdateCondition.OnLateUpdate)
            {
                UpdateTarget();
            }
        }

        private void FixedUpdate()
        {
#if UNITY_EDITOR
            if (!editMode && !Application.isPlaying)
            {
                return;
            }
#endif
            if (update == UpdateCondition.OnFixedUpdate)
            {
                UpdateTarget();
            }
        }

        private void OnValidate()
        {
            if (source != null)
            {
                source.Reset();
            }

            if (target != null)
            {
                target.Reset();
            }
        }

        /// <summary>
        /// Immediately update the bound data.
        /// </summary>
        [ContextMenu("Update Now")]
        public void UpdateTarget()
        {
            if (source != null && target != null && source.isValid && target.isValid)
            {
                if (direction == Direction.SourceUpdatesTarget)
                {
                    if (source.GetPropertyType() == typeof(bool) && target.GetPropertyType() == typeof(bool) &&
                        invertBool)
                    {
                        target.Set(!(bool) source.Get());
                    }
                    else
                    {
                        target.Set(source.Get());
                    }
                }
                else if (direction == Direction.TargetUpdatesSource)
                {
                    if (source.GetPropertyType() == typeof(bool) && target.GetPropertyType() == typeof(bool) &&
                        invertBool)
                    {
                        source.Set(!(bool) target.Get());
                    }
                    else
                    {
                        source.Set(target.Get());
                    }
                }
                else if (source.GetPropertyType() == target.GetPropertyType())
                {
                    object current = source.Get();

                    if (mLastValue == null || !mLastValue.Equals(current))
                    {
                        if (source.GetPropertyType() == typeof(bool) && target.GetPropertyType() == typeof(bool) &&
                            invertBool)
                        {
                            mLastValue = !(bool) current;
                            target.Set(!(bool) current);
                        }
                        else
                        {
                            mLastValue = current;
                            target.Set(current);
                        }
                    }
                    else
                    {
                        current = target.Get();

                        if (!mLastValue.Equals(current))
                        {
                            if (source.GetPropertyType() == typeof(bool) && target.GetPropertyType() == typeof(bool) &&
                                invertBool)
                            {
                                mLastValue = !(bool) current;
                                source.Set(!(bool) current);
                            }
                            else
                            {
                                mLastValue = current;
                                source.Set(current);
                            }
                        }
                    }
                }
            }
        }
    }
}