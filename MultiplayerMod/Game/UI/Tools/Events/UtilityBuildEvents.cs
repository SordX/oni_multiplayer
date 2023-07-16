﻿using System;
using System.Linq;
using HarmonyLib;
using MultiplayerMod.Core.Patch;

namespace MultiplayerMod.Game.UI.Tools.Events;

[HarmonyPatch(typeof(BaseUtilityBuildTool))]
public static class UtilityBuildEvents {

    public static event EventHandler<UtilityBuildEventArgs>? Build;

    [HarmonyPrefix]
    [HarmonyPatch(nameof(BaseUtilityBuildTool.BuildPath))]
    private static void BuildPathPrefix(BaseUtilityBuildTool __instance) => PatchControl.RunIfEnabled(
        () => {
            var args = new UtilityBuildEventArgs(
                __instance.def.PrefabID,
                __instance.selectedElements.ToArray(),
                __instance.path,
                GameState.BuildToolPriority
            );
            Build?.Invoke(__instance, args);
        }
    );

}
