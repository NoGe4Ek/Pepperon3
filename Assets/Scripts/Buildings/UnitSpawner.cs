using System.Collections;
using Mirror;
using Units;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Buildings {
    public class UnitSpawner : NetworkBehaviour, IPointerClickHandler {
        [SerializeField] private GameObject unitPrefab = null;
        [SerializeField] private Transform unitSpawnPoint = null;

        private float _timer;
        private bool _isSpawningCoroutineRunning = false;

        #region Server

        [Command]
        private void CmdSpawnUnit() {
            GameObject unitSpawn = Instantiate(unitPrefab, unitSpawnPoint.position, unitSpawnPoint.rotation);
            unitSpawn.tag = "Enemy";

            NetworkServer.Spawn(unitSpawn, connectionToClient);
            unitSpawn.GetComponentInParent<Unit>()
                .setGlobalPath(connectionToClient, GetDirectionVector(connectionToClient));
        }

        public Vector3 GetDirectionVector(NetworkConnectionToClient conn) {
            if (conn.connectionId == 0) {
                return new Vector3(-30, 0, 0);
            }

            return new Vector3(30, 0, 0);
        }

        #endregion

        #region Client

        public void OnPointerClick(PointerEventData eventData) {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            if (!isOwned) return;

            CmdSpawnUnit();
        }

        private void Update() {
            _timer += Time.deltaTime;

            if (!isOwned) return;

            if (!_isSpawningCoroutineRunning) {
                StartCoroutine(spawnUnit());
            }
        }

        private IEnumerator spawnUnit() {
            _isSpawningCoroutineRunning = true;
            CmdSpawnUnit();
            yield return new WaitForSeconds(5);
            _isSpawningCoroutineRunning = false;
        }

        #endregion
    }
}