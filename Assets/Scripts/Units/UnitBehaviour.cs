using System.Collections;
using System.Linq;
using Mirror;
using Units.Models;
using UnityEngine;
using UnityEngine.AI;

namespace Units {
    public class UnitBehaviour : NetworkBehaviour {
        [SerializeField] private Animator unitAnimator;
        public NavMeshAgent agent;
        public NavMeshObstacle obstacle;
        public Canvas healthBar;

        private SyncList<Vector3> path = new SyncList<Vector3>();
        private Vector3 actualPosition;
        private int corner = 1;
        private float speed = 3f;
        public bool IsStopped { get; set; } = false;
        private Unit unit;
        public bool IsCompulsion { get; set; } = false;

        [SyncVar(hook = nameof(OnUnitChangeState))]
        private UnitState unitState = new IdleUnitState();

        public UnitState getUnitState() {
            return unitState;
        }

        public bool isAttackAnimationEnd = true;
        private Quaternion _lookRotation;
        private Vector3 _direction;
        [SyncVar] private Unit _targetToRotate;
        [SyncVar] private Unit _rotationSource;

        #region Server

        [Command]
        public void CmdMoveForward(Vector3 direction) {
            if (unitState is not RunningUnitState)
                unitState = new RunningUnitState();
            //StartCoroutine(EnableNavMeshAgain());
            agent.Move(direction * agent.speed * Time.deltaTime);
            //StartCoroutine(recalculatePath(position)); 
        }

        [Server]
        public void attack() {
            if (unitState is not AttackingUnitState)
                unitState = new AttackingUnitState();
            if (agent.isActiveAndEnabled)
                StartCoroutine(DisableNavMeshAgain());
        }

        [Server]
        public void stopMovement() {
            if (unitState is not IdleUnitState)
                unitState = new IdleUnitState();
            if (agent.isActiveAndEnabled)
                StartCoroutine(DisableNavMeshAgain());
        }

        [Server]
        public void rotate(Unit toRotate, Unit target) {
            _rotationSource = toRotate;
            _targetToRotate = target;
        }

        [Server]
        public void stopRotate() {
            _rotationSource = null;
            _targetToRotate = null;
        }

        [Command]
        public void CmdDye() {
            if (unitState is not DyingUnitState) {
                unitState = new DyingUnitState();
                StartCoroutine(waitDye());
            }
        }

        [Server]
        private IEnumerator waitDye() {
            yield return new WaitForSeconds(10);
            NetworkServer.Destroy(GetComponentInParent<Unit>().gameObject);
        }

        #endregion

        #region Client

        // unitState hook callback
        public void OnUnitChangeState(UnitState oldUnitState, UnitState newUnitState) {
            if (oldUnitState is not DyingUnitState) {
                unitAnimator.SetBool(oldUnitState.PropertyName, false);
                unitAnimator.SetBool(newUnitState.PropertyName, true);
            }

            if (newUnitState is RunningUnitState) {
                //StartCoroutine(EnableNavMeshAgain());
            }

            if (newUnitState is IdleUnitState) {
                //StartCoroutine(DisableNavMeshAgain());
            }

            if (newUnitState is AttackingUnitState) {
                isAttackAnimationEnd = false;
                StartCoroutine(DisableNavMeshAgain());
            }

            if (newUnitState is DyingUnitState) {
                agent.enabled = false;
                obstacle.enabled = false;
                healthBar.enabled = false;
                foreach (Collider c in GetComponents<Collider>()) {
                    c.enabled = false;
                }

                Unit destroyedUnit = NetworkServer.spawned[netId].gameObject.GetComponentInParent<Unit>();
                GameObject[] allObjects = FindObjectsOfType<GameObject>();
                foreach (GameObject obj in allObjects) {
                    if (obj.GetComponent<Unit>() != null) {
                        Unit someComponent = obj.GetComponent<Unit>();
                        if (someComponent.UnitsInAttackRange.Contains(destroyedUnit)) {
                            someComponent.UnitsInAttackRange.Remove(destroyedUnit);
                        }

                        if (someComponent.UnitsInChaseRange.Contains(destroyedUnit)) {
                            someComponent.UnitsInChaseRange.Remove(destroyedUnit);
                        }
                    }
                }
            }
        }

        // public IEnumerator calculatePath(Vector3 position) {
        //     obstacle.enabled = false;
        //     yield return null;
        //     agent.enabled = true;
        //     NavMeshPath newPath = new NavMeshPath();
        //     agent.CalculatePath(position, newPath);
        //     path.AddRange(newPath.corners);
        //     agent.enabled = false;
        //     obstacle.enabled = true;
        // }

        public IEnumerator EnableNavMeshAgain() {
            obstacle.enabled = false;
            yield return null;
            agent.enabled = true;
            IsStopped = false;
        }

        public IEnumerator DisableNavMeshAgain() {
            agent.enabled = false;
            obstacle.enabled = true;
            IsStopped = true;
            yield return null;
        }

        public void AlertAnimationEvent(string message) {
            if (message.Equals("AttackAnimationIsEnd")) {
                stopMovement();
                isAttackAnimationEnd = true;
            }
        }

        [Server]
        private void Start() {
            NavMesh.avoidancePredictionTime = 5f;
            unit = GetComponent<Unit>();
        }

        [Server]
        private void Update() {
            if (unitState is DyingUnitState) return;

            switch (unitState) {
                case IdleUnitState: {
                    if (unit.UnitsInAttackRange.Count == 0) {
                        unit.localDestination = unit.globalDestination;
                        move(unit.localDestination);
                    }
                    else {
                        handleUnitAttack();
                    }

                    break;
                }
                case RunningUnitState: {
                    if (unit.UnitsInAttackRange.Count == 0) {
                        if (unit.UnitsInChaseRange.Count > 0) {
                            Unit unitToChase = unit.UnitsInChaseRange.First();
                            Vector3 unitToChasePosition = unitToChase.transform.position;
                            move(unitToChasePosition);
                        }
                        else {
                            if (agent.pathStatus is NavMeshPathStatus.PathInvalid) {
                                //stopMovement();
                            }
                        }
                    }
                    else {
                        handleUnitAttack();
                    }

                    Vector3 fwd = unit.transform.TransformDirection(Vector3.forward);
                    Debug.DrawRay(unit.transform.position, fwd * 5, Color.green);
                    if (Physics.Raycast(unit.transform.position, fwd, out RaycastHit objectHit, 5)) {
                        objectHit.transform.TryGetComponent(out Unit raycastedUnit);
                        if (raycastedUnit.isOwned && !raycastedUnit.GetUnitBehaviour().agent.isActiveAndEnabled) {
                            //recalculatePath();
                            //stopMovement();
                        }
                    }

                    break;
                }
                case AttackingUnitState: {
                    if (isAttackAnimationEnd) {
                        stopRotate();
                    }

                    break;
                }
            }

            if (!IsStopped) {
                if (actualPosition != path[corner]) {
                    actualPosition = transform.position;
                    unit.transform.position = Vector3.MoveTowards(actualPosition, path[corner], speed * Time.deltaTime);
                }
            }

            if (actualPosition == path[corner] && corner != path.Count - 1)
                corner++;

            if (_rotationSource != null && _targetToRotate != null) {
                if (Quaternion.Angle(_rotationSource.transform.rotation, _targetToRotate.transform.rotation) >
                    0.01f) {
                    //find the vector pointing from our position to the target
                    _direction = (_targetToRotate.transform.position - _rotationSource.transform.position)
                        .normalized;

                    //create the rotation we need to be in to look at the target
                    _lookRotation = Quaternion.LookRotation(_direction);

                    //rotate us over time according to speed until we are in the required rotation
                    transform.rotation = Quaternion.Slerp(_rotationSource.transform.rotation, _lookRotation,
                        Time.deltaTime * 2f);
                }
            }
            else {
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(path[corner]), Time.deltaTime * 30f);
            }

            if (agent.isActiveAndEnabled && unitState is not AttackingUnitState && !agent.isStopped &&
                isReachedDestination() && agent.pathStatus is not NavMeshPathStatus.PathInvalid) {
                stopMovement();
                IsCompulsion = false;
            }
        }

        private void recalculatePath() {
            if (!NavMesh.SamplePosition(unit.localDestination, out NavMeshHit hit, Mathf.Infinity, NavMesh.AllAreas))
                return;
            NavMeshPath newPath = new NavMeshPath();
            agent.CalculatePath(hit.position, newPath);
            path.AddRange(newPath.corners);
            //agent.SetPath(newPath);
        }

        private void handleUnitAttack() {
            if (unit.UnitsInAttackRange.First() != unit.unitToAttack)
                rotate(unit, unit.UnitsInAttackRange.First());

            unit.unitToAttack = unit.UnitsInAttackRange.First();

            if (agent.isActiveAndEnabled) {
                StartCoroutine(DisableNavMeshAgain());
            }

            if (unit.canAttack) {
                attack();
                unit.damage(unit.unitToAttack, 10);
                unit.canAttack = false;
            }
        }

        public void move(Vector3 position) {
            if (!NavMesh.SamplePosition(position, out NavMeshHit hit, Mathf.Infinity, NavMesh.AllAreas))
                return;
            if (!agent.isActiveAndEnabled)
                StartCoroutine(EnableNavMeshAgain());
            if (unitState is not RunningUnitState)
                unitState = new RunningUnitState();
            unit.localDestination = hit.position;
            //corner = 0;
            //path.Clear();
            recalculatePath();
            //agent.SetDestination(hit.position);
        }

        public bool isReachedDestination() {
            return !agent.pathPending && agent.pathEndPosition == unit.localDestination &&
                   agent.remainingDistance - 0.05 <= agent.stoppingDistance &&
                   (!agent.hasPath || agent.velocity.sqrMagnitude == 0f) &&
                   unitState is not IdleUnitState;
        }

        #endregion
    }
}