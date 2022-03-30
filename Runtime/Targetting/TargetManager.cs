using BlackTundra.Foundation.Collections.Generic;

using System;
using System.Collections.Generic;

using UnityEngine;

namespace BlackTundra.World.Targetting {

    /// <summary>
    /// Manages all registered <see cref="ITargetable"/> instances.
    /// </summary>
    public static class TargetManager {

        #region constant

        /// <summary>
        /// Expand size of the <see cref="TargetBuffer"/>.
        /// </summary>
        private const int TargetBufferExpandSize = 32;

        /// <summary>
        /// Buffer containing every registered <see cref="ITargetable"/> instance.
        /// </summary>
        private static readonly PackedBuffer<ITargetable> TargetBuffer = new PackedBuffer<ITargetable>(TargetBufferExpandSize);

        #endregion

        #region delegate

        /// <summary>
        /// Delegate used when defining a custom match condition for a registered <see cref="ITargetable"/> instance.
        /// </summary>
        public delegate bool TargetMatchDelegate(in ITargetable target);

        #endregion

        #region property

        /// <summary>
        /// Number of registered <see cref="ITargetable"/> instances.
        /// </summary>
        public static int RegisteredTargets => TargetBuffer.Count;

        #endregion

        #region logic

        #region IsRegistered

        /// <returns>
        /// Returns <c>true</c> if the <paramref name="target"/> is registered with the <see cref="TargetManager"/>.
        /// </returns>
        internal static bool IsRegistered(in ITargetable target) {
            if (target == null) throw new ArgumentNullException(nameof(target));
            return TargetBuffer.Contains(target);
        }

        #endregion

        #region Register

        /// <summary>
        /// Registers the <paramref name="target"/> with the <see cref="TargetManager"/>.
        /// </summary>
        /// <returns>
        /// Returns <c>true</c> if the <see cref="ITargetable"/> was not previously registered before the completion of this operation.
        /// </returns>
        internal static bool Register(in ITargetable target) {
            if (target == null) throw new ArgumentNullException(nameof(target));
            int expectedIndex = TargetBuffer.Count; // expected insersion index
            if (expectedIndex == TargetBuffer.Capacity) { // buffer is full
                TargetBuffer.Expand(TargetBufferExpandSize); // expand the buffer
            }
            return TargetBuffer.AddLast(target, true) >= expectedIndex; // if the expected insersion index matches, then the insersion was successful
        }

        #endregion

        #region Deregister

        /// <summary>
        /// Deregisters the <paramref name="target"/> from the <see cref="TargetManager"/>.
        /// </summary>
        /// <returns>
        /// Returns <c>true</c> if the <paramref name="target"/> was deregistered successfully (if it was previously registered before
        /// the method was called).
        /// </returns>
        internal static bool Deregister(in ITargetable target) {
            if (target == null) throw new ArgumentNullException(nameof(target));
            return TargetBuffer.Remove(target) > 0;
        }

        #endregion

        #region RemoveNullReferences

        /// <summary>
        /// Ensures any <c>null</c> references have been removed from the internal <see cref="TargetBuffer"/>. This may be required
        /// to be called if an <see cref="ITargetable"/> is destroyed but is not unregistered.
        /// </summary>
        public static void RemoveNullReferences() => TargetBuffer.Pack();

        #endregion

        #region FindTarget

        /// <summary>
        /// Finds any <see cref="ITargetable"/>.
        /// </summary>
        /// <remarks>
        /// This will always return the last <see cref="ITargetable"/> registered to the <see cref="TargetManager"/>.
        /// </remarks>
        public static ITargetable FindTarget() => TargetBuffer.Last;

        /// <summary>
        /// Finds the last registered <see cref="ITargetable"/> that matches the custom <paramref name="matchDelegate"/>.
        /// </summary>
        /// <param name="matchDelegate"><see cref="TargetMatchDelegate"/> used to apply a set of custom match conditions.</param>
        public static ITargetable FindTarget(in TargetMatchDelegate matchDelegate) {
            if (matchDelegate == null) throw new ArgumentNullException(nameof(matchDelegate));
            // find number of registered targets:
            int targetCount = TargetBuffer.Count;
            if (targetCount == 0) return null; // no targets are registered
            // find target with matching flags:
            ITargetable target;
            for (int i = targetCount - 1; i >= 0; i--) {
                target = TargetBuffer[i];
                if (matchDelegate.Invoke(target)) return target;
            }
            return null;
        }

        /// <summary>
        /// Finds a target with any one of the defined <paramref name="flags"/>.
        /// </summary>
        public static ITargetable FindTarget(in int flags) {
            // find number of registered targets:
            int targetCount = TargetBuffer.Count;
            if (targetCount == 0) return null; // no targets are registered
            // find target with matching flags:
            ITargetable target;
            for (int i = targetCount - 1; i >= 0; i--) {
                target = TargetBuffer[i];
                if ((target.TargetFlags & flags) != 0) return target;
            }
            return null;
        }

        /// <summary>
        /// Finds a target with any one of the defined <paramref name="flags"/> and a custom <paramref name="matchDelegate"/>.
        /// </summary>
        /// <param name="matchDelegate"><see cref="TargetMatchDelegate"/> used to apply a set of custom match conditions.</param>
        public static ITargetable FindTarget(in int flags, in TargetMatchDelegate matchDelegate) {
            if (matchDelegate == null) throw new ArgumentNullException(nameof(matchDelegate));
            // find number of registered targets:
            int targetCount = TargetBuffer.Count;
            if (targetCount == 0) return null; // no targets are registered
            // find target with matching flags:
            ITargetable target;
            for (int i = targetCount - 1; i >= 0; i--) {
                target = TargetBuffer[i];
                if ((target.TargetFlags & flags) != 0 && matchDelegate.Invoke(target)) return target;
            }
            return null;
        }

        /// <summary>
        /// Finds a single <see cref="ITargetable"/> based off of a set of <paramref name="flags"/>.
        /// The <paramref name="flags"/> are combined with the flags of each <see cref="ITargetable"/> with an
        /// AND operation. The output of this operation is then compared with the <paramref name="matchCondition"/>.
        /// If they are equal, the <see cref="ITargetable"/> is returned.
        /// </summary>
        /// <param name="flags">Isolated flags that should be tested for.</param>
        /// <param name="matchCondition">Flags that should be set out of the <paramref name="flags"/>.</param>
        public static ITargetable FindTarget(in int flags, int matchCondition) {
            // find number of registered targets:
            int targetCount = TargetBuffer.Count;
            if (targetCount == 0) return null; // no targets are registered
            // sanitize match conditions:
            matchCondition &= flags; // ensure only bits set in the flags are set in the match conditions
            // find target with matching flags:
            ITargetable target;
            for (int i = targetCount - 1; i >= 0; i--) {
                target = TargetBuffer[i];
                if ((target.TargetFlags & flags) == matchCondition) return target;
            }
            return null;
        }

        /// <summary>
        /// Finds a single <see cref="ITargetable"/> based off of a set of <paramref name="flags"/>.
        /// The <paramref name="flags"/> are combined with the flags of each <see cref="ITargetable"/> with an
        /// AND operation. The output of this operation is then compared with the <paramref name="matchCondition"/>.
        /// If they are equal, the <paramref name="matchCondition"/> will be checked; finally, if all conditions
        /// are <c>true</c>, the <see cref="ITargetable"/> is returned.
        /// </summary>
        /// <param name="flags">Isolated flags that should be tested for.</param>
        /// <param name="matchCondition">Flags that should be set out of the <paramref name="flags"/>.</param>
        /// <param name="matchDelegate"><see cref="TargetMatchDelegate"/> used to apply a set of custom match conditions.</param>
        public static ITargetable FindTarget(in int flags, int matchCondition, in TargetMatchDelegate matchDelegate) {
            if (matchDelegate == null) throw new ArgumentNullException(nameof(matchDelegate));
            // find number of registered targets:
            int targetCount = TargetBuffer.Count;
            if (targetCount == 0) return null; // no targets are registered
            // sanitize match conditions:
            matchCondition &= flags; // ensure only bits set in the flags are set in the match conditions
            // find target with matching flags:
            ITargetable target;
            for (int i = targetCount - 1; i >= 0; i--) {
                target = TargetBuffer[i];
                if ((target.TargetFlags & flags) == matchCondition && matchDelegate.Invoke(target)) return target;
            }
            return null;
        }

        #endregion

        #region FindTargets

        /// <returns>
        /// Returns every registered <see cref="ITargetable"/> instance.
        /// </returns>
        public static ITargetable[] FindTargets() => TargetBuffer.ToArray();

        /// <summary>
        /// Finds all registered <see cref="ITargetable"/> instances that matche the custom <paramref name="matchDelegate"/>.
        /// </summary>
        /// <param name="matchDelegate"><see cref="TargetMatchDelegate"/> used to apply a set of custom match conditions.</param>
        public static List<ITargetable> FindTargets(in TargetMatchDelegate matchDelegate) {
            if (matchDelegate == null) throw new ArgumentNullException(nameof(matchDelegate));
            // create list:
            List<ITargetable> list = new List<ITargetable>();
            // find number of registered targets:
            int targetCount = TargetBuffer.Count;
            if (targetCount == 0) return list; // no targets are registered
            // find targets with matching flags:
            ITargetable target;
            for (int i = targetCount - 1; i >= 0; i--) {
                target = TargetBuffer[i];
                if (matchDelegate.Invoke(target)) list.Add(target);
            }
            return list;
        }

        /// <summary>
        /// Finds a set of registered <see cref="ITargetable"/> instances with any one of the defined <paramref name="flags"/>.
        /// </summary>
        /// <remarks>
        /// This will never return <c>null</c>.
        /// </remarks>
        public static List<ITargetable> FindTargets(in int flags) {
            // create list:
            List<ITargetable> list = new List<ITargetable>();
            // find number of registered targets:
            int targetCount = TargetBuffer.Count;
            if (targetCount == 0) return list; // no targets are registered
            // find targets with matching flags:
            ITargetable target;
            for (int i = targetCount - 1; i >= 0; i--) {
                target = TargetBuffer[i];
                if ((target.TargetFlags & flags) != 0) list.Add(target);
            }
            return list;
        }

        /// <summary>
        /// Finds a set of registered <see cref="ITargetable"/> instances with any one of the defined <paramref name="flags"/>
        /// that also matches the custom <paramref name="matchDelegate"/> condition.
        /// </summary>
        /// <remarks>
        /// This will never return <c>null</c>.
        /// </remarks>
        /// <param name="matchDelegate"><see cref="TargetMatchDelegate"/> used to apply a set of custom match conditions.</param>
        public static List<ITargetable> FindTargets(in int flags, in TargetMatchDelegate matchDelegate) {
            if (matchDelegate == null) throw new ArgumentNullException(nameof(matchDelegate));
            // create list:
            List<ITargetable> list = new List<ITargetable>();
            // find number of registered targets:
            int targetCount = TargetBuffer.Count;
            if (targetCount == 0) return list; // no targets are registered
            // find targets with matching flags:
            ITargetable target;
            for (int i = targetCount - 1; i >= 0; i--) {
                target = TargetBuffer[i];
                if ((target.TargetFlags & flags) != 0 && matchDelegate.Invoke(target)) list.Add(target);
            }
            return list;
        }

        /// <summary>
        /// Finds every registered <see cref="ITargetable"/> instance based off of a set of <paramref name="flags"/>.
        /// The <paramref name="flags"/> are combined with the flags of each <see cref="ITargetable"/> with an
        /// AND operation. The output of this operation is then compared with the <paramref name="matchCondition"/>.
        /// If they are equal, the <see cref="ITargetable"/> is matched.
        /// </summary>
        /// <param name="flags">Isolated flags that should be tested for.</param>
        /// <param name="matchCondition">Flags that should be set out of the <paramref name="flags"/>.</param>
        public static List<ITargetable> FindTargets(in int flags, int matchCondition) {
            // create list:
            List<ITargetable> list = new List<ITargetable>();
            // find number of registered targets:
            int targetCount = TargetBuffer.Count;
            if (targetCount == 0) return list; // no targets are registered
            // sanitize match conditions:
            matchCondition &= flags; // ensure only bits set in the flags are set in the match conditions
            // find targets with matching flags:
            ITargetable target;
            for (int i = targetCount - 1; i >= 0; i--) {
                target = TargetBuffer[i];
                if ((target.TargetFlags & flags) == matchCondition) list.Add(target);
            }
            return list;
        }

        /// <summary>
        /// Finds every registered <see cref="ITargetable"/> instance based off of a set of <paramref name="flags"/>.
        /// The <paramref name="flags"/> are combined with the flags of each <see cref="ITargetable"/> with an
        /// AND operation. The output of this operation is then compared with the <paramref name="matchCondition"/>.
        /// If they are equal, the custom <paramref name="matchDelegate"/> condition is checked; finally, if all
        /// conditions are <c>true</c>, the <see cref="ITargetable"/> is matched and added to the list of matching
        /// <see cref="ITargetable"/> instances.
        /// </summary>
        /// <param name="flags">Isolated flags that should be tested for.</param>
        /// <param name="matchCondition">Flags that should be set out of the <paramref name="flags"/>.</param>
        /// <param name="matchDelegate"><see cref="TargetMatchDelegate"/> used to apply a set of custom match conditions.</param>
        public static List<ITargetable> FindTargets(in int flags, int matchCondition, in TargetMatchDelegate matchDelegate) {
            if (matchDelegate == null) throw new ArgumentNullException(nameof(matchDelegate));
            // create list:
            List<ITargetable> list = new List<ITargetable>();
            // find number of registered targets:
            int targetCount = TargetBuffer.Count;
            if (targetCount == 0) return list; // no targets are registered
            // sanitize match conditions:
            matchCondition &= flags; // ensure only bits set in the flags are set in the match conditions
            // find targets with matching flags:
            ITargetable target;
            for (int i = targetCount - 1; i >= 0; i--) {
                target = TargetBuffer[i];
                if ((target.TargetFlags & flags) == matchCondition && matchDelegate.Invoke(target)) list.Add(target);
            }
            return list;
        }

        #endregion

        #region FindTargetsAt

        /// <returns>
        /// Returns every registered <see cref="ITargetable"/> instance.
        /// </returns>
        public static OrderedList<float, ITargetable> FindTargetsAt(in Vector3 point, in float radius) {
            // create list:
            OrderedList<float, ITargetable> list = new OrderedList<float, ITargetable>();
            // find number of registered targets:
            int targetCount = TargetBuffer.Count;
            if (targetCount == 0) return list; // no targets are registered
            // square radius:
            float sqrRadius = radius * radius;
            // find targets with matching flags:
            ITargetable target;
            float sqrDistance;
            for (int i = targetCount - 1; i >= 0; i--) {
                target = TargetBuffer[i];
                sqrDistance = (target.position - point).sqrMagnitude;
                if (sqrDistance < sqrRadius) list.Add(sqrDistance, target);
            }
            return list;
        }

        /// <summary>
        /// Finds all registered <see cref="ITargetable"/> instances that matche the custom <paramref name="matchDelegate"/>.
        /// </summary>
        /// <param name="matchDelegate"><see cref="TargetMatchDelegate"/> used to apply a set of custom match conditions.</param>
        public static OrderedList<float, ITargetable> FindTargetsAt(in Vector3 point, in float radius, in TargetMatchDelegate matchDelegate) {
            if (matchDelegate == null) throw new ArgumentNullException(nameof(matchDelegate));
            // create list:
            OrderedList<float, ITargetable> list = new OrderedList<float, ITargetable>();
            // find number of registered targets:
            int targetCount = TargetBuffer.Count;
            if (targetCount == 0) return list; // no targets are registered
            // square radius:
            float sqrRadius = radius * radius;
            // find targets with matching flags:
            ITargetable target;
            float sqrDistance;
            for (int i = targetCount - 1; i >= 0; i--) {
                target = TargetBuffer[i];
                sqrDistance = (target.position - point).sqrMagnitude;
                if (sqrDistance < sqrRadius && matchDelegate.Invoke(target)) list.Add(sqrDistance, target);
            }
            return list;
        }

        /// <summary>
        /// Finds a set of registered <see cref="ITargetable"/> instances with any one of the defined <paramref name="flags"/>.
        /// </summary>
        /// <remarks>
        /// This will never return <c>null</c>.
        /// </remarks>
        public static OrderedList<float, ITargetable> FindTargetsAt(in Vector3 point, in float radius, in int flags) {
            // create list:
            OrderedList<float, ITargetable> list = new OrderedList<float, ITargetable>();
            // find number of registered targets:
            int targetCount = TargetBuffer.Count;
            if (targetCount == 0) return list; // no targets are registered
            // square radius:
            float sqrRadius = radius * radius;
            // find targets with matching flags:
            ITargetable target;
            float sqrDistance;
            for (int i = targetCount - 1; i >= 0; i--) {
                target = TargetBuffer[i];
                if ((target.TargetFlags & flags) != 0) {
                    sqrDistance = (target.position - point).sqrMagnitude;
                    if (sqrDistance < sqrRadius) list.Add(sqrDistance, target);
                }
            }
            return list;
        }

        /// <summary>
        /// Finds a set of registered <see cref="ITargetable"/> instances with any one of the defined <paramref name="flags"/>
        /// that also matches the custom <paramref name="matchDelegate"/> condition.
        /// </summary>
        /// <remarks>
        /// This will never return <c>null</c>.
        /// </remarks>
        /// <param name="matchDelegate"><see cref="TargetMatchDelegate"/> used to apply a set of custom match conditions.</param>
        public static OrderedList<float, ITargetable> FindTargetsAt(in Vector3 point, in float radius, in int flags, in TargetMatchDelegate matchDelegate) {
            if (matchDelegate == null) throw new ArgumentNullException(nameof(matchDelegate));
            // create list:
            OrderedList<float, ITargetable> list = new OrderedList<float, ITargetable>();
            // find number of registered targets:
            int targetCount = TargetBuffer.Count;
            if (targetCount == 0) return list; // no targets are registered
            // square radius:
            float sqrRadius = radius * radius;
            // find targets with matching flags:
            ITargetable target;
            float sqrDistance;
            for (int i = targetCount - 1; i >= 0; i--) {
                target = TargetBuffer[i];
                if ((target.TargetFlags & flags) != 0) {
                    sqrDistance = (target.position - point).sqrMagnitude;
                    if (sqrDistance < sqrRadius && matchDelegate.Invoke(target)) list.Add(sqrDistance, target);
                }
            }
            return list;
        }

        /// <summary>
        /// Finds every registered <see cref="ITargetable"/> instance based off of a set of <paramref name="flags"/>.
        /// The <paramref name="flags"/> are combined with the flags of each <see cref="ITargetable"/> with an
        /// AND operation. The output of this operation is then compared with the <paramref name="matchCondition"/>.
        /// If they are equal, the <see cref="ITargetable"/> is matched.
        /// </summary>
        /// <param name="flags">Isolated flags that should be tested for.</param>
        /// <param name="matchCondition">Flags that should be set out of the <paramref name="flags"/>.</param>
        public static OrderedList<float, ITargetable> FindTargetsAt(in Vector3 point, in float radius, in int flags, int matchCondition) {
            // create list:
            OrderedList<float, ITargetable> list = new OrderedList<float, ITargetable>();
            // find number of registered targets:
            int targetCount = TargetBuffer.Count;
            if (targetCount == 0) return list; // no targets are registered
            // square radius:
            float sqrRadius = radius * radius;
            // sanitize match conditions:
            matchCondition &= flags; // ensure only bits set in the flags are set in the match conditions
            // find targets with matching flags:
            ITargetable target;
            float sqrDistance;
            for (int i = targetCount - 1; i >= 0; i--) {
                target = TargetBuffer[i];
                if ((target.TargetFlags & flags) == matchCondition) {
                    sqrDistance = (target.position - point).sqrMagnitude;
                    if (sqrDistance < sqrRadius) list.Add(sqrDistance, target);
                }
            }
            return list;
        }

        /// <summary>
        /// Finds every registered <see cref="ITargetable"/> instance based off of a set of <paramref name="flags"/>.
        /// The <paramref name="flags"/> are combined with the flags of each <see cref="ITargetable"/> with an
        /// AND operation. The output of this operation is then compared with the <paramref name="matchCondition"/>.
        /// If they are equal, the custom <paramref name="matchDelegate"/> condition is checked; finally, if all
        /// conditions are <c>true</c>, the <see cref="ITargetable"/> is matched and added to the list of matching
        /// <see cref="ITargetable"/> instances.
        /// </summary>
        /// <param name="flags">Isolated flags that should be tested for.</param>
        /// <param name="matchCondition">Flags that should be set out of the <paramref name="flags"/>.</param>
        /// <param name="matchDelegate"><see cref="TargetMatchDelegate"/> used to apply a set of custom match conditions.</param>
        public static OrderedList<float, ITargetable> FindTargetsAt(in Vector3 point, in float radius, in int flags, int matchCondition, in TargetMatchDelegate matchDelegate) {
            if (matchDelegate == null) throw new ArgumentNullException(nameof(matchDelegate));
            // create list:
            OrderedList<float, ITargetable> list = new OrderedList<float, ITargetable>();
            // find number of registered targets:
            int targetCount = TargetBuffer.Count;
            if (targetCount == 0) return list; // no targets are registered
            // square radius:
            float sqrRadius = radius * radius;
            // sanitize match conditions:
            matchCondition &= flags; // ensure only bits set in the flags are set in the match conditions
            // find targets with matching flags:
            ITargetable target;
            float sqrDistance;
            for (int i = targetCount - 1; i >= 0; i--) {
                target = TargetBuffer[i];
                if ((target.TargetFlags & flags) == matchCondition) {
                    sqrDistance = (target.position - point).sqrMagnitude;
                    if (sqrDistance < sqrRadius && matchDelegate.Invoke(target)) list.Add(sqrDistance, target);
                }
            }
            return list;
        }

        #endregion

        #endregion

    }

}