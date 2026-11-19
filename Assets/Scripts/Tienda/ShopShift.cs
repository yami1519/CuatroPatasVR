using System;

namespace CuatroPatasVR.Shop
{
    // Reglas pequeñas e independientes de la presentación de Unity.
    public sealed class ShopShift
    {
        public enum Phase { Ready, Working, Finished }
        public Phase State { get; private set; }
        public int Completed { get; private set; }   // pedidos cerrados (pagados o perdidos)
        public int Paid { get; private set; }        // entregas correctas
        public int Lost { get; private set; }        // clientes que se fueron impacientes
        public int Mistakes { get; private set; }    // entregas equivocadas
        public int Count { get; }
        public int PayPerOrder { get; }
        public int Earned => Paid * PayPerOrder;
        public ShopShift(int count, int payPerOrder)
        {
            if (count <= 0 || payPerOrder < 0 || (long)count * payPerOrder > int.MaxValue)
                throw new ArgumentOutOfRangeException();
            Count = count;
            PayPerOrder = payPerOrder;
        }
        public bool Begin()
        {
            if (State != Phase.Ready) return false;
            State = Phase.Working;
            return true;
        }
        public bool Deliver(bool correct)
        {
            if (State != Phase.Working) return false;
            if (!correct) { Mistakes++; return false; }
            Paid++;
            Completed++;
            if (Completed == Count) State = Phase.Finished;
            return true;
        }
        public bool Skip()
        {
            if (State != Phase.Working) return false;
            Lost++;
            Completed++;
            if (Completed == Count) State = Phase.Finished;
            return true;
        }
    }
}
