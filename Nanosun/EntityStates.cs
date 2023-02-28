using EntityStates;
using RoR2;
using UnityEngine;
using EntityStates.Mage.Weapon;

namespace NanosunMod.EntityStates
{

	public class ChargeNanosun : BaseChargeBombState
	{
        public static float baseChargeDuration;

        public static GameObject areaIndicatorPrefab;

        public static GameObject novasunEffect;

        public static float minDamageCoefficient;

        public static float maxDamageCoefficient;

        public static float procCoefficient;

        public static float force;

        public static GameObject muzzleflashEffect;

        private float stopwatch;

        private GameObject areaIndicatorInstance;

        private bool fireNovasun;

        private float radius;

        private float chargeDuration;

        public override BaseThrowBombState GetNextState()
		{
			return new ThrowNanosun();
		}

        public override void OnEnter()
        {
            base.OnEnter();
            chargeDuration = this.baseDuration / base.attackSpeedStat;
            duration = this.baseDuration / base.attackSpeedStat;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            stopwatch += Time.fixedDeltaTime;
            if ((stopwatch >= duration || base.inputBank.skill2.justReleased) && base.isAuthority)
            {
                fireNovasun = true;
                outer.SetNextStateToMain();
            }
        }
    }

    public class ThrowNanosun : BaseThrowBombState
	{
        public override void OnEnter()
        {
            baseDuration = 0.5f;
            base.OnEnter();
        }
	}

}
