using BepInEx;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;

namespace Solarint.BulletCrackFix
{
    [BepInPlugin("solarint.crackFix", "BulletCrackFix", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            new BulletCrackFixPatch().Enable();
        }
    }

    internal class BulletCrackFixPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(FlyingBulletSoundPlayer).GetMethod("method_1");

        [PatchPrefix]
        public static bool Patch(EftBulletClass shot, Vector3 forward, Vector3 normal, Vector2 ____minMaxRadius, FlyingBulletSoundPlayer __instance)
        {
            float magnitude = normal.magnitude;
            if (magnitude > ____minMaxRadius.y) {
                return false;
            }
            string id = shot.Ammo.Id + shot.FragmentIndex;
            BallisticCollider hittedBallisticCollider = shot.HittedBallisticCollider;
            if (hittedBallisticCollider != null && hittedBallisticCollider.GetType() == typeof(BodyPartCollider)) {
                __instance.method_2(id);
                return false;
            }

            Player player = Singleton<GameWorld>.Instance?.GetAlivePlayerByProfileID(shot.PlayerProfileID);
            if (player == null) {
                return true;
            }
            Vector3 playerLookDir = player.LookDirection;

            Vector3 playerPos = player.Position;
            Vector3 cameraPos = CameraClass.Instance.Camera.transform.position;
            Vector3 playerDirection = (playerPos - cameraPos);
            float playerDistance = playerDirection.magnitude;
            Vector3 projectionPoint = (playerLookDir * playerDistance) + player.WeaponRoot.position;

            Vector3 projectDirection = projectionPoint - cameraPos;
            float sqrMag = projectDirection.sqrMagnitude;
            if (sqrMag > 10f * 10f) {
                return false;
            }

            if (Physics.Raycast(cameraPos, projectDirection, Mathf.Sqrt(sqrMag), LayerMaskClass.HighPolyWithTerrainMask)) {
                return false;
            }

            if (Physics.Raycast(player.WeaponRoot.position, playerLookDir, out var hit, playerDistance, LayerMaskClass.HighPolyWithTerrainMask) && (hit.point - cameraPos).sqrMagnitude > 10f * 10f) {
                return false;
            }

            return true;
        }
    }
}