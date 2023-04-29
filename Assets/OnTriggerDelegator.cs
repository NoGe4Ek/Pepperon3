using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Delegates the call to OnTrigger2D for this object to another object.
/// </summary>
public class OnTriggerDelegator : MonoBehaviour
{
    private Collider _caller;

    private void Awake()
    {
        _caller = GetComponent<Collider>();
    }

    [Tooltip("Which function should be called when trigger was entered.")]
    public UnityEvent<OnTriggerDelegation> enter;

    [Tooltip("Which function should be called when trigger was exited.")]
    public UnityEvent<OnTriggerDelegation> exit;

    void OnTriggerEnter(Collider other) => enter.Invoke(new OnTriggerDelegation(_caller, other));
    void OnTriggerExit(Collider other) => exit.Invoke(new OnTriggerDelegation(_caller, other));
}

/// <summary>
/// Stores which collider triggered this call and which collider belongs to the other object.
/// </summary>
public struct OnTriggerDelegation {

    /// <summary>
    /// Creates an OnTriggerDelegation struct.
    /// Stores which collider triggered this call and which collider belongs to the other object.
    /// </summary>
    /// <param name="caller">The trigger collider which triggered the call.</param>
    /// <param name="other">The collider which belongs to the other object.</param>
    public OnTriggerDelegation(Collider caller, Collider other)
    {
        Caller = caller;
        Other = other;
    }

    /// <summary>
    /// The trigger collider which triggered the call.
    /// </summary>
    public Collider Caller { get; private set; }

    /// <summary>
    /// The other collider.
    /// </summary>
    public Collider Other { get; private set; }
}