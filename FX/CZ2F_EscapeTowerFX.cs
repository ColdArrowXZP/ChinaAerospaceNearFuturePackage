using Smooth.Algebraics;
using System.Collections.Generic;
using UnityEngine;

namespace ChinaAeroSpaceNearFuturePackage.FX
{
    [EffectDefinition("MODEL_CASNFP_PARTICLE")]
    public class CZ2F_EscapeTowerFX : ModelMultiParticleFX
    {
        [Persistent]
        public Vector2 size = Vector2.one;
        
        private List<Transform> modelParents;

        private List<KSPParticleEmitter> emitters;

        private KSPParticleEmitter emitter;

        private float minEmission;

        private float maxEmission;

        private float minEnergy;

        private float maxEnergy;

        private Vector3 localVelocity;

        private float emissionPower;

        private float energyPower;

        private float speedPower;

        public override void OnLoad(ConfigNode node)
        {
            ConfigNode.LoadObjectFromConfig(this, node);
            emission.Load("emission", node);
            energy.Load("energy", node);
            speed.Load("speed", node);
        }

        public override void OnSave(ConfigNode node)
        {
            ConfigNode.CreateConfigFromObject(this, node);
            emission.Save(node);
            energy.Save(node);
            speed.Save(node);
        }
        public override void OnInitialize()
        {
            modelParents = new List<Transform>(hostPart.FindModelTransforms(transformName));
            if (modelParents.Count == 0)
            {
                Debug.LogError("CZ2F_EscapeTowerFX: Cannot find transform of name '" + transformName + "'");
                return;
            }
            GameObject model = GameDatabase.Instance.GetModel(modelName);
            if (model == null)
            {
                Debug.LogError("CZ2F_EscapeTowerFX: Cannot find model of name '" + modelName + "'");
                return;
            }
            model.SetActive(value: true);
            emitter = model.GetComponentInChildren<KSPParticleEmitter>();
            if (emitter == null)
            {
                Debug.LogError("CZ2F_EscapeTowerFX: Cannot find particle emitter on model of name '" + modelName + "'");
                Destroy(model);
                return;
            }
            minEmission = emitter.minEmission;
            maxEmission = emitter.maxEmission;
            minEnergy = emitter.minEnergy;
            maxEnergy = emitter.maxEnergy;
            localVelocity = emitter.localVelocity;
            if (emitters == null)
            {
                emitters = new List<KSPParticleEmitter>();
            }
            
            foreach (var item in modelParents)
            {
                GameObject gameObject = Instantiate(model);
                gameObject.transform.NestToParent(item);
                gameObject.transform.localPosition = localPosition;
                gameObject.transform.localRotation = Quaternion.Euler(localRotation);
                gameObject.transform.localScale = Vector3.Scale(gameObject.transform.localScale, localScale);
                emitter = gameObject.GetComponentInChildren<KSPParticleEmitter>();
                emitter.minSize *= size.x;
                emitter.maxSize *= size.y;
                emitters.Add(emitter);
                emitter.emit = false;
                EffectBehaviour.AddParticleEmitter(emitter);
            }
            Destroy(model);
        }
        public override void OnEvent(int transformIdx)
	{
		if (emitters == null)
		{
			return;
		}
		if (transformIdx > -1 && transformIdx < emitters.Count)
		{
			emitters[transformIdx].Emit();
			return;
		}
		int i = 0;
		for (int count = emitters.Count; i < count; i++)
		{
			emitters[i].Emit();
		}
	}

	public override void OnEvent(float power, int transformIdx)
	{
		if (emitters == null)
		{
			return;
		}
		if (power <= 0f)
		{
			if (transformIdx > -1 && transformIdx < emitters.Count)
			{
				emitters[transformIdx].emit = false;
				return;
			}
			int i = 0;
			for (int count = emitters.Count; i < count; i++)
			{
				emitters[i].emit = false;
			}
			return;
		}
		emissionPower = emission.Value(power);
		energyPower = energy.Value(power);
		speedPower = speed.Value(power);
		int minEmissionVal = Mathf.FloorToInt(minEmission * emissionPower);
		int maxEmissionVal = Mathf.FloorToInt(maxEmission * emissionPower);
		float minEnergyVal = minEnergy * energyPower;
		float maxEnergyVal = maxEnergy * energyPower;
		Vector3 localVelocityVal = localVelocity * speedPower;
		if (transformIdx > -1 && transformIdx < emitters.Count)
		{
			SetEmitter(transformIdx, minEmissionVal, maxEmissionVal, minEnergyVal, maxEnergyVal, localVelocityVal);
			return;
		}
		int j = 0;
		for (int count2 = emitters.Count; j < count2; j++)
		{
			SetEmitter(j, minEmissionVal, maxEmissionVal, minEnergyVal, maxEnergyVal, localVelocityVal);
		}
	}

	private void SetEmitter(int transformIdx, int minEmissionVal, int maxEmissionVal, float minEnergyVal, float maxEnergyVal, Vector3 localVelocityVal)
	{
		KSPParticleEmitter kSPParticleEmitter = emitters[transformIdx];
		kSPParticleEmitter.emit = true;
		kSPParticleEmitter.minEmission = minEmissionVal;
		kSPParticleEmitter.maxEmission = maxEmissionVal;
		kSPParticleEmitter.minEnergy = minEnergyVal;
		kSPParticleEmitter.maxEnergy = maxEnergyVal;
		kSPParticleEmitter.localVelocity = localVelocityVal;
	}

	private void OnDestroy()
	{
		if (emitters != null)
		{
			int i = 0;
			for (int count = emitters.Count; i < count; i++)
			{
				EffectBehaviour.RemoveParticleEmitter(emitter);
			}
		}
	}
    }
}
