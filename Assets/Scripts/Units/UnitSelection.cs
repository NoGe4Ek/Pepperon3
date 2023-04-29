using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Units
{
    public class UnitSelection : MonoBehaviour
    {
        [SerializeField] private LayerMask layerMask = new LayerMask();

        private Camera _mainCamera;
        public List<Unit> SelectedUnits { get; } = new List<Unit>();

        void Start()
        {
            _mainCamera = Camera.main;
        }

        void Update()
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                foreach (Unit selectedUnit in SelectedUnits)
                {
                    selectedUnit.Deselect();
                }
                
                SelectedUnits.Clear();
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                SelectionArea();
            }
        }

        private void SelectionArea()
        {
            Ray ray = _mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask)) return;

            if (!hit.collider.TryGetComponent<Unit>(out Unit unit)) return;

            if (!unit.isOwned) return;

            SelectedUnits.Add(unit);

            foreach (Unit selectedUnit in SelectedUnits)
            {
                selectedUnit.Select();
            }
        }
    }
}