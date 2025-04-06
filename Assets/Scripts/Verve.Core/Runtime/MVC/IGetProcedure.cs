namespace Verve.MVC
{
    public interface IGetProcedure
    {
        void SetProcedure<TProcedure>(TProcedure procedure);
        Procedure<TProcedure> GetProcedure<TProcedure>() where TProcedure : class, new();
    }
}