using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace Units
{
    public class UnitCommandManager : MonoBehaviour
    {
        [Header("Scripts")] [SerializeField] private UnitSelection unitSelection = null;

        private Camera _mainCamera;
        [SerializeField] private LayerMask layerMask = new LayerMask();

        private void Start()
        {
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            if (!Mouse.current.rightButton.wasPressedThisFrame) return;

            Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask)) return;

            MoveUnit(hit.point);
        }

        private void MoveUnit(Vector3 point)
        {
            foreach (Unit unit in unitSelection.SelectedUnits)
            {
                if (!NavMesh.SamplePosition(point, out NavMeshHit hit, Mathf.Infinity, NavMesh.AllAreas)) return;
                unit.GetUnitBehaviour().move(hit.position);
                unit.GetUnitBehaviour().IsCompulsion = true;
            }
        }
    }
}