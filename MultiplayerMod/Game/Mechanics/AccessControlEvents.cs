using System;
using HarmonyLib;
using MultiplayerMod.Core.Patch;
using MultiplayerMod.Multiplayer.Objects;

namespace MultiplayerMod.Game.Mechanics;

[HarmonyPatch(typeof(AccessControl))]
public static class AccessControlEvents {

    public static event EventHandler<AccessControlEventArgs>? DefaultPermissionChanged;
    public static event EventHandler<AccessControlEventArgs>? PermissionChanged;

    [HarmonyPostfix]
    [HarmonyPatch(nameof(AccessControl.DefaultPermission), MethodType.Setter)]
    private static void SetDefaultPermissionPostfix(AccessControl __instance, AccessControl.Permission value) =>
        PatchControl.RunIfEnabled(
            () => DefaultPermissionChanged?.Invoke(
                __instance,
                new AccessControlEventArgs(
                    null,
                    __instance.GetGridReference(),
                    value
                )
            )
        );

    [HarmonyPostfix]
    [HarmonyPatch(nameof(AccessControl.SetPermission))]
    private static void SetPermissionPostfix(
        AccessControl __instance,
        MinionAssignablesProxy key,
        AccessControl.Permission permission
    ) => PatchControl.RunIfEnabled(
        () => PermissionChanged?.Invoke(
            __instance,
            new AccessControlEventArgs(
                key.GetMultiplayerReference(),
                __instance.GetGridReference(),
                permission
            )
        )
    );

    [HarmonyPostfix]
    [HarmonyPatch(nameof(AccessControl.ClearPermission))]
    private static void ClearPermissionPostifx(AccessControl __instance, MinionAssignablesProxy key) =>
        PatchControl.RunIfEnabled(
            () => PermissionChanged?.Invoke(
                __instance,
                new AccessControlEventArgs(
                    key.GetMultiplayerReference(),
                    __instance.GetGridReference(),
                    null
                )
            )
        );

}
