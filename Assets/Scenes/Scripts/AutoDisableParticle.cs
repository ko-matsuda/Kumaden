using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class AutoDisableParticle : MonoBehaviour
{
    void Awake()
    {
        var ps = GetComponent<ParticleSystem>();
        var main = ps.main;
        main.stopAction = ParticleSystemStopAction.Disable;
    }
}
