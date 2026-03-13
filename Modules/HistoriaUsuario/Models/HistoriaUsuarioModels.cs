using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Automatizacion.Agentes.Modules.HistoriaUsuario.Models
{
    public class Requerimiento
    {
        [JsonPropertyName("Nombre_del_Proceso")]
        [JsonConverter(typeof(Core.Converters.StringOrArrayConverter))]
        public string NombreProceso { get; set; } = "";

        [JsonPropertyName("Personas_Asistentes")]
        [JsonConverter(typeof(Core.Converters.StringOrArrayConverter))]
        public string Asistentes { get; set; } = "";

        /// <summary>
        /// ¿Qué se quiere hacer? - Explica el valor o problema que se está resolviendo.
        /// </summary>
        [JsonPropertyName("¿Qué se quiere hacer?")]
        [JsonConverter(typeof(Core.Converters.StringOrArrayConverter))]
        public string QueSeQuiereHacer { get; set; } = "";

        /// <summary>
        /// ¿Para qué sirve o por qué se necesita? - El valor que resuelve la implementación.
        /// </summary>
        [JsonPropertyName("¿Para qué sirve o por qué se necesita?")]
        [JsonConverter(typeof(Core.Converters.StringOrArrayConverter))]
        public string ParaQueSirve { get; set; } = "";

        /// <summary>
        /// ¿Cómo debería funcionar? - Describe cómo el usuario imagina o espera que funcione.
        /// </summary>
        [JsonPropertyName("¿Cómo debería funcionar?")]
        [JsonConverter(typeof(Core.Converters.StringOrArrayConverter))]
        public string ComoDeberiaFuncionar { get; set; } = "";

        /// <summary>
        /// ¿Qué se necesita para que funcione? - Lista los insumos: archivos, formatos, reglas, campos, etc.
        /// </summary>
        [JsonPropertyName("¿Qué se necesita para que funcione?")]
        [JsonConverter(typeof(Core.Converters.StringOrArrayConverter))]
        public string QueSeNecesita { get; set; } = "";

        /// <summary>
        /// Criterios de Aceptación - Condiciones claras que permiten validar que está completo.
        /// </summary>
        [JsonPropertyName("Criterios de Aceptación")]
        [JsonConverter(typeof(Core.Converters.StringOrArrayConverter))]
        public string CriteriosAceptacion { get; set; } = "";

        [JsonPropertyName("Diagrama_C4")]
        [JsonConverter(typeof(Core.Converters.StringOrArrayConverter))]
        public string PlantUml { get; set; } = "";
    }

    /// <summary>
    /// Wrapper para deserializar la respuesta que viene como array directo de la IA.
    /// </summary>
    public class RespuestaHistoriaUsuario
    {
        [JsonPropertyName("proyectos")]
        public List<Requerimiento> Proyectos { get; set; } = new();
    }
}
