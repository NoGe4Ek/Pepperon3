using System.Collections;
using Mirror;
using Units.Models;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Units {
    public class Unit : NetworkBehaviour {
        [SerializeField] private Slider healthBar;
        [SerializeField] private UnityEvent onSelected = null;
        [SerializeField] private UnityEvent onDeselected = null;

        [SyncVar(hook = nameof(OnHitPointsChangeState))]
        private int hitPoints = 100;

        private float _attackSpeed = 0.4f;

        private bool isAttackDelayCoroutineRunning;
        [SyncVar] public bool canAttack;
        public Unit unitToAttack;

        [SyncVar] public Vector3 globalDestination;
        [SyncVar] public Vector3 localDestination;
        private bool isMoveToDestination = true;
        public SyncList<Unit> UnitsInChaseRange { get; } = new SyncList<Unit>();

        public SyncList<Unit> UnitsInAttackRange { get; } = new SyncList<Unit>();

        [Header("Scripts")] [SerializeField] private UnitBehaviour unitBehaviour;

        public UnitBehaviour GetUnitBehaviour() {
            return unitBehaviour;
        }

        #region Server

        [Server]
        public void damage(Unit unitToDamage, int damage) {
            unitToDamage.hitPoints -= damage;
        }

        [Server]
        public void setGlobalPath(NetworkConnectionToClient con, Vector3 position) {
            globalDestination = position;
            localDestination = position;
        }

        #endregion

        #region Client

        // hook callback
        public void OnHitPointsChangeState(int oldHitPoints, int newHitPoints) {
            healthBar.value = newHitPoints;
            if (!isOwned) return;
            if (hitPoints <= 0) {
                GetUnitBehaviour().CmdDye();
            }
        }

        [Server]
        public void OnAttackTriggerEnter(OnTriggerDelegation delegation) {
            if (delegation.Caller.name != "AttackRangeSphere") return;
            if (!delegation.Other.TryGetComponent(out Unit otherUnit)) return;
            Unit callerUnit = delegation.Caller.GetComponentInParent<Unit>();

            if (otherUnit.connectionToClient != callerUnit.connectionToClient) {
                Debug.Log("Trigger attack Enter");
                UnitsInAttackRange.Add(otherUnit);
            }
        }

        [Server]
        public void OnAttackTriggerExit(OnTriggerDelegation delegation) {
            if (delegation.Caller.name != "AttackRangeSphere") return;
            if (!delegation.Other.TryGetComponent(out Unit otherUnit)) return;
            Unit callerUnit = delegation.Caller.GetComponentInParent<Unit>();

            if (otherUnit.connectionToClient != callerUnit.connectionToClient) {
                Debug.Log("Trigger attack Exit");
                UnitsInAttackRange.Remove(otherUnit);
            }
        }

        [Server]
        public void OnChaseTriggerEnter(OnTriggerDelegation delegation) {
            if (delegation.Caller.name != "ChaseSphere") return;
            if (!delegation.Other.TryGetComponent(out Unit otherUnit)) return;
            Unit callerUnit = delegation.Caller.GetComponentInParent<Unit>();

            if (otherUnit.connectionToClient != callerUnit.connectionToClient) {
                Debug.Log("Trigger chase Enter");
                UnitsInChaseRange.Add(otherUnit);
            }
        }

        [Server]
        public void OnChaseTriggerExit(OnTriggerDelegation delegation) {
            if (delegation.Caller.name != "ChaseSphere") return;
            if (!delegation.Other.TryGetComponent(out Unit otherUnit)) return;
            Unit callerUnit = delegation.Caller.GetComponentInParent<Unit>();

            if (otherUnit.connectionToClient != callerUnit.connectionToClient) {
                Debug.Log("Trigger chase Exit");
                UnitsInChaseRange.Remove(otherUnit);
            }
        }

        [Server]
        private void Update() {
            //if (!isOwned) return;
            if (GetUnitBehaviour().getUnitState() is DyingUnitState) return;
            if (GetUnitBehaviour().IsCompulsion) return;

            UnitsInAttackRange.RemoveAll(unit => unit.GetUnitBehaviour().getUnitState() is DyingUnitState);
            UnitsInChaseRange.RemoveAll(unit => unit.GetUnitBehaviour().getUnitState() is DyingUnitState);

            if (!canAttack && !isAttackDelayCoroutineRunning) {
                StartCoroutine(attackDelayCoroutine());
            }

            // if (UnitsInAttackRange.Count == 0 && GetUnitBehaviour().getUnitState() is IdleUnitState) {
            //     //recalculatePath();
            // }

            // if (!GetUnitBehaviour().agent.isActiveAndEnabled && UnitsInAttackRange.Count == 0 &&
            //     GetUnitBehaviour().agent.pathStatus is NavMeshPathStatus.PathInvalid) {
            //     recalculatePath();
            //     //GetUnitBehaviour().agent.autoRepath = true;
            //     //GetUnitBehaviour().agent.autoRepath = false;
            // }

            // 
            // 
        }

        IEnumerator attackDelayCoroutine() {
            isAttackDelayCoroutineRunning = true;
            yield return new WaitForSeconds(1 / _attackSpeed);
            canAttack = true;
            isAttackDelayCoroutineRunning = false;
        }

        [Client]
        public void Select() {
            if (!isOwned) return;

            onSelected?.Invoke();
        }

        [Client]
        public void Deselect() {
            if (!isOwned) return;

            onDeselected?.Invoke();
        }

        #endregion
    }
}