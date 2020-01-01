using System;
using UnityEngine;

/// <summary>
/// This class acts as a delegate for collision events, as Unity does not
/// allow a way for listening to those unless through a MonoBehaviour.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CollisionDelegate : MonoBehaviour {

    public event EventHandler<Collision> CollisionEnter;
    public event EventHandler<Collision> CollisionExit;
    public event EventHandler<Collision> CollisionStay;
    public event EventHandler<Collider> TriggerEnter;
    public event EventHandler<Collider> TriggerExit;
    public event EventHandler<Collider> TriggerStay;

    private void OnCollisionEnter(Collision collision) {
        CollisionEnter?.Invoke(this, collision);
    }

    private void OnCollisionExit(Collision collision) {
        CollisionExit?.Invoke(this, collision);
    }

    private void OnCollisionStay(Collision collision) {
        CollisionStay?.Invoke(this, collision);
    }

    private void OnTriggerEnter(Collider collider) {
        TriggerEnter?.Invoke(this, collider);
    }

    private void OnTriggerExit(Collider collider) {
        TriggerExit?.Invoke(this, collider);
    }

    private void OnTriggerStay(Collider collider) {
        TriggerStay?.Invoke(this, collider);
    }
}