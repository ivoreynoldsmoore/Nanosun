using BepInEx;
using EntityStates;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using On.RoR2.Skills;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Projectile;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using EntityStates.Mage.Weapon;
using UnityEngine.UIElements;
using System.Security.Cryptography.X509Certificates;
using UnityEngine.Events;
using UnityEngine.Experimental.GlobalIllumination;

namespace NanosunMod
{
    public static class Assets
    {
        public static GameObject NanosunPrefab;

        private static GameObject nanosunGhostPrefab;

        private static float nanosunRadius;

        public static void PopulateAssets()
        {
            CreateProjectile();
        }

        private static void CreateProjectile()
        {
            nanosunRadius = 6f;
            NanosunPrefab = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Croco/CrocoSlash.prefab").WaitForCompletion(), "NanosunProjectile", true);
            NanosunPrefab.transform.position = new Vector3(3.8f,-7.1f, 7.3f);
            Nanosun.Destroy(NanosunPrefab.GetComponent<ProjectileFuse>());

            CreateGhost();

            // Import for assets
            GameObject sunPrefab = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Grandparent/GrandParentSun.prefab").WaitForCompletion(), "sunPrefab", false);

            // Prepping explosion effect
            //GameObject sunSpawnPrefab = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Grandparent/GrandParentSunSpawn.prefab").WaitForCompletion(), "sunSpawnPrefab", false);
            //Nanosun.Destroy(sunSpawnPrefab.GetComponent<DestroyOnTimer>());

            GrandParentSunController sunController = sunPrefab.GetComponent<GrandParentSunController>();

            ProjectileController controller = NanosunPrefab.GetComponent<ProjectileController>();
            controller.ghostPrefab = nanosunGhostPrefab;
            //controller.flightSoundLoop = sunController.activeLoopDef;

            //Nanosun.Destroy(sunPrefab);

            Rigidbody body = NanosunPrefab.GetComponent<Rigidbody>();
            body.drag = 0f;

            ProjectileSimple projectile = NanosunPrefab.GetComponent<ProjectileSimple>();
            projectile.lifetime = 5f;
            projectile.desiredForwardSpeed = 15f;

            ProjectileExplosion explosion = NanosunPrefab.GetComponent<ProjectileExplosion>();
            explosion.blastProcCoefficient = 0.5f;
            //explosion.explosionEffect = sunSpawnPrefab;

            SphereCollider collider = NanosunPrefab.GetComponent<SphereCollider>();
            collider.radius = 1f;


            HitBox[] hitBox = NanosunPrefab.GetComponentsInChildren<HitBox>();
            Transform transform1 = hitBox[0].GetComponent<Transform>();
            transform1.localScale = new Vector3(nanosunRadius * 2f, nanosunRadius * 2f, nanosunRadius * 2f);
            Transform transform2 = hitBox[1].GetComponent<Transform>();
            transform2.localScale = new Vector3(nanosunRadius * 2f, nanosunRadius * 2f, nanosunRadius * 2f);
            Transform transform3 = hitBox[2].GetComponent<Transform>();
            transform3.localScale = new Vector3(nanosunRadius * 2f, nanosunRadius * 2f, nanosunRadius * 2f);

            ProjectileDotZone dotZone = NanosunPrefab.GetComponent<ProjectileDotZone>();
            dotZone.attackerFiltering = AttackerFiltering.Default;
            dotZone.forceVector = new Vector3(0f, 0f, 0f);
            dotZone.lifetime = 5f;
            dotZone.damageCoefficient = 1f;
            dotZone.fireFrequency = 8f;
            dotZone.resetFrequency = 2f;
            dotZone.overlapProcCoefficient = 0.5f;
            dotZone.impactEffect = sunController.buffApplyEffect;

            ContentAddition.AddProjectile(NanosunPrefab);
        }

        private static void CreateGhost()
        {
            // Prepping projectile ghost
            nanosunGhostPrefab = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Grandparent/GrandParentSun.prefab").WaitForCompletion(), "NanosunGhost", true);
            nanosunGhostPrefab.AddComponent<ProjectileGhostController>();
            ProjectileGhostController controller = nanosunGhostPrefab.GetComponent<ProjectileGhostController>();

            Nanosun.Destroy(nanosunGhostPrefab.GetComponent<GrandParentSunController>());
            Nanosun.Destroy(nanosunGhostPrefab.GetComponent<TeamFilter>());
            Nanosun.Destroy(nanosunGhostPrefab.GetComponent<GenericOwnership>());
            Nanosun.Destroy(nanosunGhostPrefab.GetComponent<EntityStateMachine>());
            Nanosun.Destroy(nanosunGhostPrefab.GetComponent<Deployable>());
            Nanosun.Destroy(nanosunGhostPrefab.GetComponent<Rigidbody>());

            // Modify desired components
            Transform[] transforms = nanosunGhostPrefab.GetComponentsInChildren<Transform>();
            Transform areaIndicator = transforms[7];
            areaIndicator.localScale = new Vector3(nanosunRadius, nanosunRadius, nanosunRadius);

            Transform vfxRoot = transforms[1];

            Light[] lights = nanosunGhostPrefab.GetComponentsInChildren<Light>();
            Light pointLight = lights[0];
            pointLight.range = nanosunRadius;
            pointLight.intensity = 600f;
            pointLight.transform.SetParent(vfxRoot);

            Transform trails = transforms[18];
            trails.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            // Remove unwanted components and set the rest as children of the main transform.
            GameObject junkObject = new GameObject();

            int[] indicesToKeep = new int[] {
                0, 1, 5, 6, 7, 10, 18
            };
            for (int i = 0; i < transforms.Length; i++)
            {
                Debug.Log("Tranform " + i + " name: " + transforms[i].name);
                if (!indicesToKeep.Contains(i))
                {
                    Debug.Log("Discarded!");
                    transforms[i].SetParent(junkObject.transform);
                }
            }

            Nanosun.Destroy(junkObject);

            transforms = nanosunGhostPrefab.GetComponentsInChildren<Transform>();
            //sunTransforms[10].localScale = new Vector3(5f, 5f, 5f);
            //sunTransforms[11].localScale = new Vector3(0.5f, 0.5f, 0.5f);
            //sunTransforms[14].localScale = new Vector3(0.5f, 0.5f, 0.5f);
            //sunTransforms[16].localScale = new Vector3(0.5f, 0.5f, 0.5f);
            //sunTransforms[18].localScale = new Vector3(0.5f, 0.5f, 0.5f);

            //Add shell particles
            GameObject flamethrowerEffect1 = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mage/MageFlamethrowerEffect.prefab").WaitForCompletion(), "Flamethrower1", false);
            ParticleSystem[] flamethrowerComponents = flamethrowerEffect1.GetComponentsInChildren<ParticleSystem>();
            ParticleSystem outerEffect = flamethrowerComponents[2];
            Nanosun.Destroy(flamethrowerEffect1);

            ParticleSystem.MainModule outerEffectMain = outerEffect.main;
            outerEffectMain.duration = 1f;
            outerEffectMain.startLifetime = 5f;
            outerEffect.transform.localPosition = new Vector3(0f, 0f, 0f);
            outerEffect.transform.SetParent(nanosunGhostPrefab.transform);
            outerEffect.transform.localPosition = new Vector3(0f, 0f, 0f);
            outerEffect.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);


            GameObject flamethrowerEffect2 = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mage/MageFlamethrowerEffect.prefab").WaitForCompletion(), "Flamethrower2", false);
            flamethrowerComponents = flamethrowerEffect2.GetComponentsInChildren<ParticleSystem>();
            ParticleSystem innerEffect = flamethrowerComponents[2];
            Nanosun.Destroy(flamethrowerEffect2);

            ParticleSystem.MainModule innerEffectMain = innerEffect.main;
            innerEffectMain.duration = 1f;
            innerEffectMain.startLifetime = 5f;
            innerEffectMain.flipRotation = 1f;
            innerEffect.transform.localPosition = new Vector3(0f, 0f, 0f);
            innerEffect.transform.SetParent(nanosunGhostPrefab.transform);
            innerEffect.transform.localPosition = new Vector3(0f, 0f, 0f);
            innerEffect.transform.localScale = new Vector3(0.5f, 0.4f, 0.4f);


            CreateParticles();
        }

        private static void CreateParticles()
        {
            // Generate emitters with trails pointing in different directions
            GameObject flamethrowerEffect;
            Quaternion[] rotations = new Quaternion[]
            {
                Quaternion.Euler(new Vector3(0f, 0f, 0f)),
                Quaternion.Euler(new Vector3(0f, 90f, 0f)),
                Quaternion.Euler(new Vector3(0f, 180f, 0f)),
                Quaternion.Euler(new Vector3(0f, 270f, 0f)),
                Quaternion.Euler(new Vector3(90f, 0f, 0f)),
                Quaternion.Euler(new Vector3(270f, 0f, 0f))

            };
            for (int i = 0; i < rotations.Length; i++)
            {
                flamethrowerEffect = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mage/MageFlamethrowerEffect.prefab").WaitForCompletion(), "Flamethrower", false);
                ParticleSystem[] flameParticles = flamethrowerEffect.GetComponentsInChildren<ParticleSystem>();
                ParticleSystem trailSparks = flameParticles[1];
                Nanosun.Destroy(flamethrowerEffect);

                ParticleSystem.MainModule trailMain = trailSparks.main;
                trailMain.duration = 5f;

                //ParticleSystem.MinMaxCurve size = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
                //ParticleSystem.MinMaxCurve speed = new ParticleSystem.MinMaxCurve(10f, 20f);
                //trailMain.startSize = size;
                //Debug.Log(trailMain.startSpeed.constant);
                //trailMain.startSpeed = speed;

                //ParticleSystem.TrailModule particleTrails = trailSparks.trails;
                //particleTrails.mode = ParticleSystemTrailMode.Ribbon;
                //particleTrails.ratio = 0.8f;
                //particleTrails.sizeAffectsWidth = true;

                ParticleSystem.EmissionModule emission = trailSparks.emission;
                emission.rateOverTime = 7;

                ParticleSystem.ShapeModule shape = trailSparks.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.radius = 1f;
                shape.angle = 90f;
                shape.randomDirectionAmount = 1f;

                trailSparks.transform.localRotation = rotations[i];
                trailSparks.transform.SetParent(nanosunGhostPrefab.transform);
                trailSparks.transform.localPosition = new Vector3(0f, 0f, 0f);
            }
        }
    }
}
