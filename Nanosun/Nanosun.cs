using BepInEx;
using EntityStates;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Projectile;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using EntityStates.Mage.Weapon;

namespace NanosunMod
{
    //This is an example plugin that can be put in BepInEx/plugins/ExamplePlugin/ExamplePlugin.dll to test out.
    //It's a small plugin that adds a relatively simple item to the game, and gives you that item whenever you press F2.

    //This attribute specifies that we have a dependency on R2API, as we're using it to add our item to the game.
    //You don't need this if you're not using R2API in your plugin, it's just to tell BepInEx to initialize R2API before this plugin so it's safe to use R2API.
    [BepInDependency(R2API.R2API.PluginGUID)]

    //This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    //We will be using 2 modules from R2API: ItemAPI to add our item and LanguageAPI to add our language tokens.
    [R2APISubmoduleDependency(nameof(ItemAPI), nameof(LanguageAPI))]

    //This is the main declaration of our plugin class. BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    //BaseUnityPlugin itself inherits from MonoBehaviour, so you can use this as a reference for what you can declare and use in your plugin class: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public class Nanosun : BaseUnityPlugin
    {
        //The Plugin GUID should be a unique ID for this plugin, which is human readable (as it is used in places like the config).
        //If we see this PluginGUID as it is on thunderstore, we will deprecate this mod. Change the PluginAuthor and the PluginName !
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Haggleman";
        public const string PluginName = "Nanosun";
        public const string PluginVersion = "0.0.1";

        //The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            //Init our logging class so that we can properly log for debugging
            Log.Init(Logger);

            AddTokens();
            Assets.PopulateAssets();
            AddEntityStates();
            AddSkill();

            Hooks();

            // This line of log will appear in the bepinex console when the Awake method is done.
            Log.LogInfo(nameof(Awake) + " done.");
        }

        private void Hooks()
        {
            //AdjustFire();
        }

        private void AdjustFire()
        {
            IL.EntityStates.Mage.Weapon.BaseThrowBombState.OnEnter += (il) =>
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(MoveType.Before,
                    x => x.MatchLdarg(0),
                    x => x.MatchCallOrCallvirt<BaseThrowBombState>("Fire"),
                    x => x.MatchRet()
                    );
                c.Index += 1;
                c.Prev.OpCode = OpCodes.Nop;
                c.Next.OpCode = OpCodes.Nop;
            };
        }


        //This function adds the tokens from the item using LanguageAPI, the comments in here are a style guide, but is very opiniated. Make your own judgements!
        private void AddTokens()
        {
            //The Name should be self explanatory
            LanguageAPI.Add("MAGE_SECONDARY_NANOSUN_NAME", "Hurl Nano-Sun");

            //The Description is where you put the actual numbers and give an advanced description.
            LanguageAPI.Add("MAGE_SECONDARY_NANOSUN_DESCRIPTION", "<style=cIsDamage>Ignite.</style> Charge up a <style=cIsDamage>lingering</style> nano-sun that deals <style=cIsDamage>400%-2400%</style> damage");
        }

        private void AddSkill()
        {
            GameObject MageBodyPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mage/MageBody.prefab").WaitForCompletion();
            SkillLocator skillLocator = MageBodyPrefab.GetComponent<SkillLocator>();
            RoR2.Skills.SkillFamily skillFamily = skillLocator.secondary.skillFamily;

            int skillIndex = 0;
            RoR2.Skills.SkillDef nanobomb = skillFamily.variants[skillIndex].skillDef;
            RoR2.Skills.SkillDef nanosun = ScriptableObject.CreateInstance<RoR2.Skills.SkillDef>();

            nanosun.activationState = new SerializableEntityStateType(typeof(ChargeNanosun));
            nanosun.activationStateMachineName = nanobomb.activationStateMachineName;
            nanosun.baseMaxStock = nanobomb.baseMaxStock;
            nanosun.baseRechargeInterval = nanobomb.baseRechargeInterval;
            nanosun.beginSkillCooldownOnSkillEnd = nanobomb.beginSkillCooldownOnSkillEnd;
            nanosun.canceledFromSprinting = nanobomb.canceledFromSprinting;
            nanosun.cancelSprintingOnActivation = nanobomb.cancelSprintingOnActivation;
            nanosun.fullRestockOnAssign = nanobomb.fullRestockOnAssign;
            nanosun.interruptPriority = nanobomb.interruptPriority;
            nanosun.isCombatSkill = nanobomb.isCombatSkill;
            nanosun.mustKeyPress= nanobomb.mustKeyPress;
            nanosun.rechargeStock = nanobomb.rechargeStock;
            nanosun.requiredStock= nanobomb.requiredStock;
            nanosun.stockToConsume= nanobomb.stockToConsume;
            nanosun.icon = nanobomb.icon;
            nanosun.skillName = "MAGE_SECONDARY_NANOSUN_NAME";
            nanosun.skillNameToken = "MAGE_SECONDARY_NANOSUN_NAME";
            nanosun.skillDescriptionToken = "MAGE_SECONDARY_NANOSUN_DESCRIPTION";

            ContentAddition.AddSkillDef(nanosun);



            Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
            skillFamily.variants[skillFamily.variants.Length - 1] = new RoR2.Skills.SkillFamily.Variant
            {
                skillDef = nanosun,
                unlockableDef = ScriptableObject.CreateInstance<UnlockableDef>(),
                viewableNode = new ViewablesCatalog.Node(nanosun.skillNameToken, false, null)
            };
        }

        private void AddEntityStates()
        {
            ContentAddition.AddEntityState<ChargeNanosun>(out _);
            ContentAddition.AddEntityState<ThrowNanosun>(out _);
        }
    }

    

    public class ChargeNanosun : BaseChargeBombState
    {
        public static float baseChargeDuration;

        public static GameObject nanosunEffect;

        public static float procCoefficient;

        public static float force;

        public static GameObject muzzleflashEffect;

        public override void OnEnter()
        {
            base.OnEnter();
            baseDuration = 2f;
            minBloomRadius = 0f;
            maxBloomRadius = 0.5f;
        }

        public override BaseThrowBombState GetNextState()
        {
            return new ThrowNanosun();
        }
    }

    public class ThrowNanosun : BaseThrowBombState
    {
        public static float ignitePercentChance;

        public override void OnEnter()
        {
            duration = baseDuration / attackSpeedStat;
            projectilePrefab = Assets.NanosunPrefab;
            ignitePercentChance = 0.5f;
            minDamageCoefficient = 1.2f;
            maxDamageCoefficient = 6f;

            base.OnEnter();
        }

        public override void ModifyProjectile(ref FireProjectileInfo projectileInfo)
        {
            projectileInfo.damageTypeOverride = Util.CheckRoll(ignitePercentChance, base.characterBody.master) ? DamageType.IgniteOnHit : DamageType.Generic;
        }
    }
}
