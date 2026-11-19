// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.PlayerLoop;
using UnityEngine.LowLevel;

namespace Unity.Entities
{
    /// <summary>
    /// Contains helpers to add and remove systems to the UnityEngine player loop.
    /// </summary>
    public static class ScriptBehaviourUpdateOrder
    {
        internal static int PlayerLoopVersion => PlayerLoop.Version;

        delegate bool RemoveFromPlayerLoopDelegate(ref PlayerLoopSystem playerLoop);

        /// <summary>
        /// Append the update function to the matching player loop system type.
        /// </summary>
        /// <param name="updateType">The update function type.</param>
        /// <param name="updateFunction">The update function.</param>
        /// <param name="playerLoop">The player loop.</param>
        /// <param name="playerLoopSystemType">The player loop system type.</param>
        /// <returns><see langword="true"/> if successfully appended to player loop, <see langword="false"/> otherwise.</returns>
        internal static bool AppendToPlayerLoop(Type updateType, PlayerLoopSystem.UpdateFunction updateFunction, ref PlayerLoopSystem playerLoop, Type playerLoopSystemType)
        {
            return AppendToPlayerLoopList(updateType, updateFunction, ref playerLoop, playerLoopSystemType);
        }

        /// <summary>
        /// Append the update function to the current player loop matching system type.
        /// </summary>
        /// <remarks>
        /// The player loop is not updated when failing to find the player loop system type.
        /// </remarks>
        /// <param name="updateType">The type of the update function.</param>
        /// <param name="updateFunction">The update function.</param>
        /// <param name="playerLoopSystemType">The player loop system type to add to.</param>
        /// <returns><see langword="true"/> if successfully appended to player loop, <see langword="false"/> otherwise.</returns>
        internal static bool AppendToCurrentPlayerLoop(Type updateType, PlayerLoopSystem.UpdateFunction updateFunction, Type playerLoopSystemType)
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            if (!AppendToPlayerLoop(updateType, updateFunction, ref playerLoop, playerLoopSystemType))
                return false;

            PlayerLoop.SetPlayerLoop(playerLoop);
            return true;
        }

        /// <summary>
        /// Determine if the update function is part of the player loop.
        /// </summary>
        /// <param name="updateFunction">The update function.</param>
        /// <param name="playerLoop">The player loop.</param>
        /// <returns><see langword="true"/> if update function is part of player loop, <see langword="false"/> otherwise.</returns>
        internal static bool IsInPlayerLoop(PlayerLoopSystem.UpdateFunction updateFunction, ref PlayerLoopSystem playerLoop)
        {
            return IsInPlayerLoopList(updateFunction, ref playerLoop);
        }

        /// <summary>
        /// Determine if the update function is part of the current player loop.
        /// </summary>
        /// <param name="updateFunction">The update function.</param>
        /// <param name="playerLoop">The player loop.</param>
        /// <returns><see langword="true"/> if update function is part of player loop, <see langword="false"/> otherwise.</returns>
        internal static bool IsInCurrentPlayerLoop(PlayerLoopSystem.UpdateFunction updateFunction)
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            return IsInPlayerLoopList(updateFunction, ref playerLoop);
        }

        /// <summary>
        /// Remove the update function from the player loop.
        /// </summary>
        /// <param name="updateFunction">The update function.</param>
        /// <param name="playerLoop">The player loop.</param>
        /// <returns><see langword="true"/> if successfully removed from player loop, <see langword="false"/> otherwise.</returns>
        internal static bool RemoveFromPlayerLoop(PlayerLoopSystem.UpdateFunction updateFunction, ref PlayerLoopSystem playerLoop)
        {
            return RemoveFromPlayerLoopList(updateFunction, ref playerLoop);
        }

        /// <summary>
        /// Remove the update function from the current player loop.
        /// </summary>
        /// <param name="updateFunction">The update function.</param>
        internal static void RemoveFromCurrentPlayerLoop(PlayerLoopSystem.UpdateFunction updateFunction)
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            if (RemoveFromPlayerLoop(updateFunction, ref playerLoop))
                PlayerLoop.SetPlayerLoop(playerLoop);
        }

        /// <summary>
        /// Add this World's three default top-level system groups to a PlayerLoopSystem object.
        /// </summary>
        /// <remarks>
        /// This function performs the following modifications to the provided PlayerLoopSystem:
        /// - If an instance of InitializationSystemGroup exists in this World, it is appended to the
        ///   Initialization player loop phase.
        /// - If an instance of SimulationSystemGroup exists in this World, it is appended to the
        ///   Update player loop phase.
        /// - If an instance of PresentationSystemGroup exists in this World, it is appended to the
        ///   PreLateUpdate player loop phase.
        /// If instances of any or all of these system groups don't exist in this World, then no entry is added to the player
        /// loop for that system group.
        ///
        /// This function does not change the currently active player loop. If this behavior is desired, it's necessary
        /// to call PlayerLoop.SetPlayerLoop(playerLoop) after the systems have been removed.
        /// </remarks>
        /// <param name="world">The three top-level system groups from this World will be added to the provided player loop.</param>
        /// <param name="playerLoop">Existing player loop to modify (e.g.  (e.g. PlayerLoop.GetCurrentPlayerLoop())</param>
        public static void AppendWorldToPlayerLoop(World world, ref PlayerLoopSystem playerLoop)
        {
            if (world == null)
                return;

            var initGroup = world.GetExistingSystemManaged<InitializationSystemGroup>();
            if (initGroup != null)
                AppendSystemToPlayerLoop(initGroup, ref playerLoop, typeof(Initialization));

            var simGroup = world.GetExistingSystemManaged<SimulationSystemGroup>();
            if (simGroup != null)
                AppendSystemToPlayerLoop(simGroup, ref playerLoop, typeof(Update));

            var presGroup = world.GetExistingSystemManaged<PresentationSystemGroup>();
            if (presGroup != null)
                AppendSystemToPlayerLoop(presGroup, ref playerLoop, typeof(PreLateUpdate));
        }

        /// <summary>
        /// Append this World's three default top-level system groups to the current Unity player loop.
        /// </summary>
        /// <remarks>
        /// This is a convenience wrapper around AddWorldToPlayerLoop() that retrieves the current player loop,
        /// adds a World's top-level system groups to it, and sets the modified copy as the new active player loop.
        ///
        /// Note that modifications to the active player loop do not take effect until to the next iteration through the player loop.
        /// </remarks>
        /// <param name="world">The three top-level system groups from this World will be added to the provided player loop.</param>
        public static void AppendWorldToCurrentPlayerLoop(World world)
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            AppendWorldToPlayerLoop(world, ref playerLoop);
            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        /// <summary>
        /// Search the provided player loop for any systems added by this World.
        /// </summary>
        /// <remarks>
        /// Note that systems are not added to the player loop directly; they are wrapped by a DummyDelegate object that
        /// calls the system's Update() method. Any systems added to the loop using other wrapper mechanisms will not
        /// be detected by this function.
        /// </remarks>
        /// <param name="world">The function will search the provided PlayerLoopSystem for systems owned by this World.</param>
        /// <param name="playerLoop">Existing player loop to search (e.g. PlayerLoop.GetCurrentPlayerLoop())</param>
        /// <returns>True if any of this World's systems are found in the provided player loop; otherwise, false.</returns>
        public static bool IsWorldInPlayerLoop(World world, PlayerLoopSystem playerLoop)
        {
            return IsWorldInPlayerLoopList(world, ref playerLoop);
        }

        /// <summary>
        /// Search the currently active player loop for any systems added by this World.
        /// </summary>
        /// <remarks>
        /// This is a convenience wrapper around IsWorldInPlayerLoop() that always searches the currently active player loop.
        /// </remarks>
        /// <param name="world">The function will search the currently active player loop for systems owned by this World.</param>
        /// <returns>True if all of <paramref name="world"/>'s default system groups are in the player loop, or false if not.</returns>
        public static bool IsWorldInCurrentPlayerLoop(World world)
        {
            return IsWorldInPlayerLoop(world, PlayerLoop.GetCurrentPlayerLoop());
        }

        /// <summary>
        /// Remove all of this World's systems from the specified player loop.
        /// </summary>
        /// <remarks>
        /// Only the systems from this World will be removed; other player loop modifications (including systems added
        /// by other Worlds) will not be affected.
        ///
        /// This function does not change the currently active player loop. If this behavior is desired, it's necessary
        /// to call PlayerLoop.SetPlayerLoop(playerLoop) after the systems have been removed.
        /// </remarks>
        /// <param name="world">All systems in the provided player loop owned by this World will be removed from the player loop.</param>
        /// <param name="playerLoop">Existing player loop to modify (e.g. PlayerLoop.GetCurrentPlayerLoop())</param>
        public static void RemoveWorldFromPlayerLoop(World world, ref PlayerLoopSystem playerLoop)
        {
            RemoveWorldFromPlayerLoopList(world, ref playerLoop);
        }

        /// <summary>
        /// Remove all of this World's systems from the currently active player loop.
        /// </summary>
        /// <remarks>
        /// This is a convenience wrapper around RemoveWorldToPlayerLoop() that retrieves the current player loop,
        /// removes a World's systems from it, and sets the modified copy as the new active player loop.
        ///
        /// Note that modifications to the active player loop do not take effect until to the next iteration through the player loop.
        /// </remarks>
        /// <param name="world">All systems in the current player loop owned by this World will be removed from the player loop.</param>
        public static void RemoveWorldFromCurrentPlayerLoop(World world)
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            RemoveWorldFromPlayerLoop(world, ref playerLoop);
            PlayerLoop.SetPlayerLoop(playerLoop);
        }

        /// <summary>
        /// Add an ECS system to a specific point in the Unity player loop, so that it is updated every frame.
        /// </summary>
        /// <remarks>
        /// This function does not change the currently active player loop. If this behavior is desired, it's necessary
        /// to call PlayerLoop.SetPlayerLoop(playerLoop) after the systems have been removed.
        /// </remarks>
        /// <param name="system">The ECS system to add to the player loop.</param>
        /// <param name="playerLoop">Existing player loop to modify (e.g. PlayerLoop.GetCurrentPlayerLoop())</param>
        /// <param name="playerLoopSystemType">The Type of the PlayerLoopSystem subsystem to which the ECS system should be appended.
        /// See the UnityEngine.PlayerLoop namespace for valid values.</param>
        public static void AppendSystemToPlayerLoop(ComponentSystemBase system, ref PlayerLoopSystem playerLoop, Type playerLoopSystemType)
        {
            var wrapper = new DummyDelegateWrapper(system);
            if (!AppendToPlayerLoop(system.GetType(), wrapper.TriggerUpdate, ref playerLoop, playerLoopSystemType))
                throw new ArgumentException($"Could not find PlayerLoopSystem with type={playerLoopSystemType}");
        }

        static bool AppendToPlayerLoopList(Type updateType, PlayerLoopSystem.UpdateFunction updateFunction, ref PlayerLoopSystem playerLoop, Type playerLoopSystemType)
        {
            if (updateType == null || updateFunction == null || playerLoopSystemType == null)
                return false;

            if (playerLoop.type == playerLoopSystemType)
            {
                var oldListLength = playerLoop.subSystemList != null ? playerLoop.subSystemList.Length : 0;
                var newSubsystemList = new PlayerLoopSystem[oldListLength + 1];
                for (var i = 0; i < oldListLength; ++i)
                    newSubsystemList[i] = playerLoop.subSystemList[i];
                newSubsystemList[oldListLength] = new PlayerLoopSystem
                {
                    type = updateType,
                    updateDelegate = updateFunction
                };
                playerLoop.subSystemList = newSubsystemList;
                return true;
            }

            if (playerLoop.subSystemList != null)
            {
                for (var i = 0; i < playerLoop.subSystemList.Length; ++i)
                {
                    if (AppendToPlayerLoopList(updateType, updateFunction, ref playerLoop.subSystemList[i], playerLoopSystemType))
                        return true;
                }
            }
            return false;
        }

        static bool IsInPlayerLoopList(PlayerLoopSystem.UpdateFunction updateFunction, ref PlayerLoopSystem playerLoop)
        {
            if (updateFunction == null)
                return false;

            for (var i = 0; i < playerLoop.subSystemList.Length; ++i)
            {
                if (IsInPlayerLoopList(updateFunction, ref playerLoop.subSystemList[i]))
                    return true;
            }

            return playerLoop.updateDelegate == updateFunction;
        }

        static bool IsWorldInPlayerLoopList(World world, ref PlayerLoopSystem playerLoop)
        {
            if (world == null || !world.IsCreated)
                return false;

            // Is *this* system a delegate for a component system associated with the current World?
            if (IsDelegateForWorldSystem(world, playerLoop))
                return true;

            // How about anything in the subsystem list?
            if (playerLoop.subSystemList != null)
            {
                for (int i = 0; i < playerLoop.subSystemList.Length; ++i)
                {
                    if (IsWorldInPlayerLoopList(world, ref playerLoop.subSystemList[i]))
                        return true;
                }
            }

            // Nope.
            return false;
        }

        static bool RemoveFromPlayerLoopList(RemoveFromPlayerLoopDelegate removeDelegate, ref PlayerLoopSystem playerLoop)
        {
            if (removeDelegate == null || playerLoop.subSystemList == null || playerLoop.subSystemList.Length == 0)
                return false;

            var result = false;
            var newSubSystemList = new List<PlayerLoopSystem>(playerLoop.subSystemList.Length);
            for (var i = 0; i < playerLoop.subSystemList.Length; ++i)
            {
                ref var playerLoopSubSystem = ref playerLoop.subSystemList[i];
                result |= RemoveFromPlayerLoopList(removeDelegate, ref playerLoopSubSystem);
                if (!removeDelegate(ref playerLoopSubSystem))
                    newSubSystemList.Add(playerLoopSubSystem);
            }

            if (newSubSystemList.Count != playerLoop.subSystemList.Length)
            {
                playerLoop.subSystemList = newSubSystemList.ToArray();
                result = true;
            }
            return result;
        }

        static bool RemoveFromPlayerLoopList(PlayerLoopSystem.UpdateFunction updateFunction, ref PlayerLoopSystem playerLoop)
        {
            return RemoveFromPlayerLoopList((ref PlayerLoopSystem pl) => pl.updateDelegate == updateFunction, ref playerLoop);
        }

        static void RemoveWorldFromPlayerLoopList(World world, ref PlayerLoopSystem playerLoop)
        {
            RemoveFromPlayerLoopList((ref PlayerLoopSystem pl) => IsDelegateForWorldSystem(world, pl), ref playerLoop);
        }

        internal static bool IsDelegateForWorldSystem(World world, in PlayerLoopSystem playerLoop)
        {
            if (typeof(ComponentSystemBase).IsAssignableFrom(playerLoop.type))
            {
                var wrapper = playerLoop.updateDelegate.Target as DummyDelegateWrapper;
                if (wrapper.System.World == world)
                    return true;
            }
            return false;
        }

        // FIXME: HACK! - mono 4.6 has problems invoking virtual methods as delegates from native, so wrap the invocation in a non-virtual class
        internal class DummyDelegateWrapper
        {
            internal ComponentSystemBase System => m_System;
            private readonly ComponentSystemBase m_System;

            public DummyDelegateWrapper(ComponentSystemBase sys)
            {
                m_System = sys;
            }

            public unsafe void TriggerUpdate()
            {
                if (m_System.m_StatePtr != null)
                {
                    m_System.Update();
                }
            }
        }
    }
}
