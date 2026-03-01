using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Automatizacion.AgentesKoncilia.Modules.HistoriaUsuario.Models
{
    public class RequerimientoKoncilia
    {
        [JsonPropertyName("Nombre_del_Proceso")]
        [JsonConverter(typeof(Core.Converters.StringOrArrayConverter))]
        public string NombreProceso { get; set; } = "";

        [JsonPropertyName("Personas_Asistentes")]
        [JsonConverter(typeof(Core.Converters.StringOrArrayConverter))]
        public string Asistentes { get; set; } = "";

        [JsonPropertyName("Objetivo_Especifico")]
        [JsonConverter(typeof(Core.Converters.StringOrArrayConverter))]
        public string Objetivo { get; set; } = "";

        [JsonPropertyName("Justificacion_de_Negocio")]
        [JsonConverter(typeof(Core.Converters.StringOrArrayConverter))]
        public string Justificacion { get; set; } = "";

        [JsonPropertyName("Alcance_Funcional_Clave")]
        [JsonConverter(typeof(Core.Converters.StringOrArrayConverter))]
        public string Alcance { get; set; } = "";

        [JsonPropertyName("Dependencias_y_Fuentes")]
        [JsonConverter(typeof(Core.Converters.StringOrArrayConverter))]
        public string Dependencias { get; set; } = "";

        [JsonPropertyName("Criterios_de_Aceptacion")]
        [JsonConverter(typeof(Core.Converters.StringOrArrayConverter))]
        public string CriteriosBrutos { get; set; } = "";

        [JsonPropertyName("Resumen_Ejecutivo_del_Alcance")]
        [JsonConverter(typeof(Core.Converters.StringOrArrayConverter))]
        public string ResumenEjecutivo { get; set; } = "";

        [JsonPropertyName("Flujo_Funcional_Alto_Nivel")]
        [JsonConverter(typeof(Core.Converters.StringOrArrayConverter))]
        public string FlujoFuncional { get; set; } = "";

        [JsonPropertyName("Diagrama_C4")]
        [JsonConverter(typeof(Core.Converters.StringOrArrayConverter))]
        public string PlantUml { get; set; } = "";
    }

    /// <summary>
    /// Wrapper para deserializar la respuesta que viene dentro de "proyectos"
    /// </summary>
    public class RespuestaHistoriaUsuario
    {
        [JsonPropertyName("proyectos")]
        public List<RequerimientoKoncilia> Proyectos { get; set; } = new();
    }
}
