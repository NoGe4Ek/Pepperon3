namespace Units.Models {
    public class UnitState {
        public virtual string PropertyName { get; set; } = null;
    }

    public class IdleUnitState : UnitState {
        public override string PropertyName { get; set; } = "isIdle";
    }

    public class RunningUnitState : UnitState {
        public override string PropertyName { get; set; } = "isRunning";
    }

    public class AggressionUnitState : UnitState {
        public override string PropertyName { get; set; } = "isAgression";
    }

    public class AttackingUnitState : UnitState {
        public override string PropertyName { get; set; } = "isAttacking";
    }

    public class DyingUnitState : UnitState {
        public override string PropertyName { get; set; } = "isDying";
    }
}