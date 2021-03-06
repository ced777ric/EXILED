// -----------------------------------------------------------------------
// <copyright file="Stopping.cs" company="Exiled Team">
// Copyright (c) Exiled Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events.Patches.Events.Warhead
{
#pragma warning disable SA1313
    using System;

    using Exiled.Events.EventArgs;
    using Exiled.Events.Handlers;

    using HarmonyLib;

    using Mirror;

    using UnityEngine;

    /// <summary>
    /// Patches <see cref="AlphaWarheadController.CancelDetonation(GameObject)"/>.
    /// Adds the <see cref="Warhead.Stopping"/> event.
    /// </summary>
    [HarmonyPatch(typeof(AlphaWarheadController), nameof(AlphaWarheadController.CancelDetonation), new Type[] { typeof(GameObject) })]
    internal class Stopping
    {
        private static bool Prefix(AlphaWarheadController __instance, GameObject disabler)
        {
            ServerLogs.AddLog(ServerLogs.Modules.Warhead, "Detonation cancelled.", ServerLogs.ServerLogType.GameEvent);

            if (!__instance.inProgress || __instance.timeToDetonation <= 10.0)
                return false;

            if (__instance.timeToDetonation <= 15.0 && disabler != null)
                __instance.GetComponent<PlayerStats>().TargetAchieve(disabler.GetComponent<NetworkIdentity>().connectionToClient, "thatwasclose");

            var ev = new StoppingEventArgs(API.Features.Player.Get(disabler) ?? API.Features.Server.Host);

            Warhead.OnStopping(ev);

            return ev.IsAllowed && !API.Features.Warhead.IsWarheadLocked;
        }
    }
}
