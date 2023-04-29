using System;
using Mirror;
using Units.Models;

namespace Networking.Serializers {
    public static class UnitStateSerializer {
        const byte IDLE = 1;
        const byte RUNNING = 2;
        const byte AGGRESSION = 3;
        const byte ATTACKING = 4;
        const byte DYING = 5;

        public static void WriteUnitState(this NetworkWriter writer, UnitState unitState) {
            switch (unitState) {
                case IdleUnitState:
                    writer.WriteByte(IDLE);
                    break;
                case RunningUnitState runningUnitState:
                    writer.WriteByte(RUNNING);
                    break;
                case AggressionUnitState:
                    writer.WriteByte(AGGRESSION);
                    break;
                case AttackingUnitState:
                    writer.WriteByte(ATTACKING);
                    break;
                case DyingUnitState:
                    writer.WriteByte(DYING);
                    break;
            }
        }

        public static UnitState ReadUnitState(this NetworkReader reader) {
            byte type = reader.ReadByte();
            return type switch {
                IDLE => new IdleUnitState(),
                RUNNING => new RunningUnitState(),
                AGGRESSION => new AggressionUnitState(),
                ATTACKING => new AttackingUnitState(),
                DYING => new DyingUnitState(),
                _ => throw new Exception($"Invalid UnitState type {type}")
            };
        }
    }
}