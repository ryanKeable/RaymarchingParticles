using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
// using Editor.c

//TODO: connections scalar is set to 1 for now
//TODO: myabe have a seperate list of connections again, one PER connection part to draw seperately 
// TODO: essenetially we need a system where each SDF controls its own union smoothness
//TODO: for spheres this is their scale percent
//TODO: for cylinders this is their lerpValue
[Serializable]
public class ParticleNode
{
    public uint id;
    public int index;

    public Vector3 particlePosition;
    public float particleScale;
    public float particleScalePercent;
    public float particleRemainingLife;
    public float particleLifePercent;
    public float connectionScalar = 1f;
    public bool isAlive; // stops us looking for new connections when dead, or being found
    public bool isMature; // stops us looking for new connections when dead, or being found
    public bool isDying; // stops us looking for new connections when dead, or being found

    public List<uint> connectionIDs = new List<uint>();
    public int totalConnections { get => connectionIDs.Count; }

    // 
    private ParticleNode[] closeNodes;

    public float totalConnectionScalar = 0;

    private float startScale;
    private float startLife;


    float maturePercent = 0.15f;//{ get => connectionScalar * 0.25f; }

    public Vector4 ParticleNodePos()
    {
        return new Vector4(particlePosition.x, particlePosition.y, particlePosition.z, 0);
    }

    public Vector4 ParticleNodeScalars()
    {
        // *Mathf.Pow(connectionScalar, totalConnectionsCount); // do this afterwards??
        float scale = particleScale;
        // scale /= totalConnectionScalar;

        particleScalePercent = ScalePercent();
        float scalePercent = Utils.CappedSmootheness(particleScalePercent); // any value over the cap, we want to be one
        scalePercent /= totalConnectionScalar;

        return new Vector4(scale, scalePercent, 0, 0);
    }

    public ParticleNode(ParticleSystem.Particle _particle)
    {
        id = _particle.randomSeed;
    }

    public void AddConnection(uint _id)
    {
        connectionIDs.Add(_id);
    }

    public void RemoveConnection(uint _id)
    {
        connectionIDs.Remove(_id);
    }

    public void SetIndex(int _index)
    {
        index = _index;
    }

    public void UpdateParticleNode(ParticleSystem.Particle[] _activeParticles, out ParticleNode nodeToRemove)
    {
        nodeToRemove = null;

        ParticleSystem.Particle? theParticle;
        var matchingParticles = _activeParticles.Where(p => p.randomSeed == id).ToArray();
        if (matchingParticles.Length == 0) theParticle = null;
        else theParticle = matchingParticles.FirstOrDefault();

        isAlive = theParticle != null;

        particlePosition = GetParticlePosition(theParticle);
        particleScale = GetParticleScales(theParticle);
        particleRemainingLife = GetParticleLifeValues(theParticle);
        particleLifePercent = LifePercent();
        isDying = IsDying();
        isMature = IsMature();

        ClampTotalConnectionScalar(0);

        if (!isAlive) nodeToRemove = this;
    }

    public ParticleConnection FindCloseConnections(ParticleNode[] _activeNodes)
    {
        if (!isMature || totalConnections >= Utils.MaxConnections) return null;

        closeNodes = Utils.FindCloseParticleNodes(_activeNodes, this);
        if (closeNodes == null || closeNodes.Length == 0) return null;

        return Utils.CheckToAddConnections(this, closeNodes.ToArray());
    }


    public void AdjustScalePerConnection(float lerpValue)
    {
        // we are trying to account for the growrth that occurs when we union two SDFs in the chader
        if (totalConnections == 0)
        {
            connectionScalar = 1f;
            return;
        }

        float targetScalar = 1 - Utils.Smoothness * totalConnections;
        float elapsedTime = 1.0f;
        if (connectionScalar != targetScalar)
        {
            elapsedTime = 0.0f;
            elapsedTime += lerpValue;
        }

        float scaleTarget = Mathf.Lerp(connectionScalar, targetScalar, elapsedTime);
        connectionScalar = scaleTarget;
        connectionScalar = 1f;
    }

    private Vector3 GetParticlePosition(ParticleSystem.Particle? _particle)
    {
        if (_particle == null) return particlePosition;
        return Utils.ParticlePostionToWorld((ParticleSystem.Particle)_particle);
    }

    private float GetParticleScales(ParticleSystem.Particle? _particle)
    {
        if (_particle == null) return 0;
        startScale = Utils.ParticleStartSizeToLocalTransform((ParticleSystem.Particle)_particle);// / Mathf.Max(totalConnections, 1);
        return Utils.CurrentParticleSizeToLocalTransform((ParticleSystem.Particle)_particle);// / Mathf.Max(totalConnections, 1);
    }

    private float GetParticleLifeValues(ParticleSystem.Particle? _particle)
    {
        if (_particle == null) return 0;
        ParticleSystem.Particle p = (ParticleSystem.Particle)_particle;
        startLife = p.startLifetime;
        return p.remainingLifetime;
    }

    private bool IsMature()
    {
        return particleLifePercent > maturePercent && !isDying;
    }

    private bool IsDying()
    {
        return particleLifePercent > (1 - maturePercent);
    }

    private float LifePercent()
    {
        // remaninig life counts backwards so we must invert it
        return (startLife - particleRemainingLife) / startLife;
    }

    private float ScalePercent()
    {
        return particleScale / startScale;
    }

    public void ConnectionScales(float value)
    {
        value /= 2;
        totalConnectionScalar += value; // add 0-> value 3 times?
        ClampTotalConnectionScalar(totalConnectionScalar);
    }

    private void ClampTotalConnectionScalar(float value)
    {
        totalConnectionScalar = Mathf.Max(value, 1);
    }

}