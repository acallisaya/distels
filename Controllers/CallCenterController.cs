using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using distels.Models;
using distels.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace distels.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CallCenterController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CallCenterController> _logger;

        public CallCenterController(
            ApplicationDbContext context,
            ILogger<CallCenterController> logger)
        {
            _context = context;
            _logger = logger;
        }



        // 2. OBTENER LLAMADAS PENDIENTES PARA EJECUCIÓN
        [HttpGet("pendientes")]
        public async Task<ActionResult<IEnumerable<object>>> GetLlamadasPendientes()
        {
            try
            {
                // Usar DateTime.UtcNow en lugar de DateTime.Now
                var ahoraUtc = DateTime.UtcNow;

                var llamadas = await _context.LlamadasIA
                    .Include(l => l.Activacion)
                        .ThenInclude(a => a.ClienteFinal)
                    .Include(l => l.Vendedor)
                    .Where(l => l.Estado == "PROGRAMADA"
                             && l.FechaProgramada <= ahoraUtc  // Usar UTC
                             && l.IntentoNumero <= l.MaxIntentos)
                    .OrderBy(l => l.FechaProgramada)
                    .Take(10)
                    .Select(l => new
                    {
                        l.Id,
                        l.ActivacionId,
                        Vendedor = new { l.Vendedor.Id, l.Vendedor.Nombre, l.Vendedor.Celular },
                        ClienteFinal = new { l.ClienteFinal.Id, l.ClienteFinal.Nombre, l.ClienteFinal.Celular },
                        l.TipoLlamada,
                        l.Estado,
                        FechaProgramada = l.FechaProgramada,
                        l.IntentoNumero,
                        l.MaxIntentos
                    })
                    .ToListAsync();

                return Ok(llamadas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetLlamadasPendientes");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // 3. OBTENER TODAS LAS LLAMADAS CON FILTROS
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetLlamadas(
     [FromQuery] int? vendedorId = null,
     [FromQuery] string? estado = null,
     [FromQuery] string? fechaDesde = null,  // Cambia a string
     [FromQuery] string? fechaHasta = null,  // Cambia a string
     [FromQuery] int page = 1,
     [FromQuery] int pageSize = 20)
        {
            var query = _context.LlamadasIA
                .Include(l => l.ClienteFinal)
                .Include(l => l.Vendedor)
                .AsQueryable();

            if (vendedorId.HasValue)
                query = query.Where(l => l.VendedorId == vendedorId.Value);

            if (!string.IsNullOrEmpty(estado))
                query = query.Where(l => l.Estado == estado);

            // Manejo UTC para fechas
            if (!string.IsNullOrEmpty(fechaDesde) && DateTime.TryParse(fechaDesde, out DateTime fechaDesdeParsed))
            {
                var fechaDesdeUtc = fechaDesdeParsed.ToUniversalTime();
                query = query.Where(l => l.FechaCreacion >= fechaDesdeUtc);
            }

            if (!string.IsNullOrEmpty(fechaHasta) && DateTime.TryParse(fechaHasta, out DateTime fechaHastaParsed))
            {
                var fechaHastaUtc = fechaHastaParsed.ToUniversalTime();
                query = query.Where(l => l.FechaCreacion <= fechaHastaUtc);
            }

            var total = await query.CountAsync();

            var llamadas = await query
                .OrderByDescending(l => l.FechaCreacion)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new
                {
                    l.Id,
                    l.ActivacionId,
                    Vendedor = new { l.Vendedor.Id, l.Vendedor.Nombre },
                    ClienteFinal = new { l.ClienteFinal.Id, l.ClienteFinal.Nombre, l.ClienteFinal.Celular },
                    l.TipoLlamada,
                    l.Estado,
                    l.FechaProgramada,
                    l.FechaEjecucion,
                    l.Resultado,
                    l.Sentimiento,
                    l.DuracionSegundos,
                    l.IntentoNumero,
                    l.FechaCreacion
                })
                .ToListAsync();

            return Ok(new
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Data = llamadas
            });
        }

        // 4. OBTENER UNA LLAMADA ESPECÍFICA
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetLlamada(int id)
        {
            var llamada = await _context.LlamadasIA
                .Include(l => l.Activacion)
                .Include(l => l.ClienteFinal)
                .Include(l => l.Vendedor)
                .Include(l => l.Respuestas)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (llamada == null)
                return NotFound(new { message = "Llamada no encontrada" });

            return Ok(new
            {
                llamada.Id,
                llamada.ActivacionId,
                Vendedor = new { llamada.Vendedor.Id, llamada.Vendedor.Nombre, llamada.Vendedor.Celular, llamada.Vendedor.Email },
                ClienteFinal = new { llamada.ClienteFinal.Id, llamada.ClienteFinal.Nombre, llamada.ClienteFinal.Celular, llamada.ClienteFinal.Email },
                llamada.TipoLlamada,
                llamada.Estado,
                llamada.FechaProgramada,
                llamada.FechaEjecucion,
                llamada.Resultado,
                llamada.Sentimiento,
                llamada.Satisfaccion,
                llamada.DuracionSegundos,
                llamada.GrabacionUrl,
                llamada.TwilioCallSid,
                llamada.TranscripcionCompleta,
                llamada.IntentoNumero,
                llamada.MaxIntentos,
                Respuestas = llamada.Respuestas.Select(r => new
                {
                    r.Id,
                    r.PreguntaId,
                    r.PreguntaTexto,
                    r.RespuestaCliente,
                    r.CategoriaRespuesta,
                    r.Sentimiento,
                    r.RequiereSeguimiento,
                    r.Tags,
                    r.FechaRegistro
                }),
                llamada.FechaCreacion,
                llamada.FechaActualizacion
            });
        }

        // 5. ESTADÍSTICAS DEL CALL CENTER
        [HttpGet("estadisticas")]
        public async Task<ActionResult<object>> GetEstadisticas(
     [FromQuery] int? vendedorId = null)
        {
            try
            {
                var query = _context.LlamadasIA.AsQueryable();

                if (vendedorId.HasValue)
                    query = query.Where(l => l.VendedorId == vendedorId.Value);

                var totalLlamadas = await query.CountAsync();
                var completadas = await query.Where(l => l.Estado == "COMPLETADA").CountAsync();
                var fallidas = await query.Where(l => l.Estado == "FALLIDA").CountAsync();
                var programadas = await query.Where(l => l.Estado == "PROGRAMADA").CountAsync();
                var enCurso = await query.Where(l => l.Estado == "EN_CURSO").CountAsync();

                var tasaExito = totalLlamadas > 0 ? Math.Round((completadas * 100.0) / totalLlamadas, 2) : 0;

                // Por resultado
                var llamadasPorResultado = await query
                    .Where(l => !string.IsNullOrEmpty(l.Resultado))
                    .GroupBy(l => l.Resultado)
                    .Select(g => new { Resultado = g.Key, Cantidad = g.Count() })
                    .ToListAsync();

                // Por sentimiento
                var llamadasPorSentimiento = await query
                    .Where(l => !string.IsNullOrEmpty(l.Sentimiento))
                    .GroupBy(l => l.Sentimiento)
                    .Select(g => new { Sentimiento = g.Key, Cantidad = g.Count() })
                    .ToListAsync();

                // Últimos 7 días - CORREGIDO
                var fechaLimite = DateTime.UtcNow.AddDays(-7);
                var ultimos7DiasQuery = await query
                    .Where(l => l.FechaCreacion >= fechaLimite)
                    .GroupBy(l => l.FechaCreacion.Date)
                    .Select(g => new
                    {
                        Fecha = g.Key,
                        Cantidad = g.Count()
                    })
                    .ToListAsync();

                var ultimos7Dias = ultimos7DiasQuery
                    .OrderBy(g => g.Fecha)
                    .Select(d => new
                    {
                        Fecha = d.Fecha.ToString("yyyy-MM-dd"),
                        Cantidad = d.Cantidad
                    })
                    .ToList();

                // Por hora - CORREGIDO
                var porHoraQuery = await query
                    .Where(l => l.FechaEjecucion.HasValue)
                    .Select(l => new { Hora = l.FechaEjecucion.Value.Hour })
                    .ToListAsync();

                var porHora = porHoraQuery
                    .GroupBy(x => x.Hora)
                    .Select(g => new { Hora = g.Key, Cantidad = g.Count() })
                    .OrderBy(g => g.Hora)
                    .ToList();

                return Ok(new
                {
                    Totales = new
                    {
                        TotalLlamadas = totalLlamadas,
                        Completadas = completadas,
                        Fallidas = fallidas,
                        Programadas = programadas,
                        EnCurso = enCurso,
                        TasaExito = tasaExito,
                        LlamadasHoy = await query.Where(l => l.FechaCreacion.Date == DateTime.UtcNow.Date).CountAsync()
                    },
                    Distribucion = new
                    {
                        PorResultado = llamadasPorResultado,
                        PorSentimiento = llamadasPorSentimiento
                    },
                    Tendencia = new
                    {
                        Ultimos7Dias = ultimos7Dias,
                        PorHora = porHora
                    },
                    Tiempos = new
                    {
                        PromedioDuracion = totalLlamadas > 0 ?
                            await query.Where(l => l.DuracionSegundos.HasValue)
                                .AverageAsync(l => l.DuracionSegundos.Value) : 0
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetEstadisticas");
                return StatusCode(500, new { error = ex.Message, details = ex.StackTrace });
            }
        }

        // 6. OBTENER LLAMADAS POR VENDEDOR
        [HttpGet("por-vendedor/{vendedorId}")]
        public async Task<ActionResult<object>> GetLlamadasPorVendedor(int vendedorId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.LlamadasIA
                .Include(l => l.ClienteFinal)
                .Where(l => l.VendedorId == vendedorId);

            var total = await query.CountAsync();

            var llamadas = await query
                .OrderByDescending(l => l.FechaCreacion)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new
                {
                    l.Id,
                    l.ActivacionId,
                    ClienteFinal = new { l.ClienteFinal.Id, l.ClienteFinal.Nombre, l.ClienteFinal.Celular },
                    l.Estado,
                    l.Resultado,
                    l.Sentimiento,
                    l.FechaProgramada,
                    l.FechaEjecucion,
                    l.DuracionSegundos,
                    l.IntentoNumero,
                    l.FechaCreacion
                })
                .ToListAsync();

            // Estadísticas específicas para este vendedor
            var estadisticasVendedor = new
            {
                Total = total,
                Completadas = await query.Where(l => l.Estado == "COMPLETADA").CountAsync(),
                Fallidas = await query.Where(l => l.Estado == "FALLIDA").CountAsync(),
                TasaExito = total > 0 ? Math.Round((await query.Where(l => l.Estado == "COMPLETADA").CountAsync() * 100.0) / total, 2) : 0
            };

            return Ok(new
            {
                Estadisticas = estadisticasVendedor,
                Llamadas = new
                {
                    Total = total,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                    Data = llamadas
                }
            });
        }

        // 7. REPROGRAMAR LLAMADA
        [HttpPut("reprogramar/{id}")]
        public async Task<IActionResult> ReprogramarLlamada(int id, [FromBody] ReprogramarLlamadaRequest request)
        {
            var llamada = await _context.LlamadasIA.FindAsync(id);
            if (llamada == null)
                return NotFound(new { message = "Llamada no encontrada" });

            llamada.Estado = "PROGRAMADA";
            llamada.FechaProgramada = request.NuevaFecha;
            llamada.FechaActualizacion = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Llamada reprogramada exitosamente",
                fecha = llamada.FechaProgramada,
                estado = llamada.Estado
            });
        }

        // 8. CANCELAR LLAMADA
        [HttpPut("cancelar/{id}")]
        public async Task<IActionResult> CancelarLlamada(int id)
        {
            var llamada = await _context.LlamadasIA.FindAsync(id);
            if (llamada == null)
                return NotFound(new { message = "Llamada no encontrada" });

            llamada.Estado = "CANCELADA";
            llamada.FechaActualizacion = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Llamada cancelada exitosamente",
                estado = llamada.Estado
            });
        }

        // 9. CREAR/MODIFICAR SCRIPT DE LLAMADA
        [HttpPost("scripts")]
        public async Task<ActionResult<ScriptLlamada>> CrearScript([FromBody] ScriptLlamada script)
        {
            if (script.VendedorId.HasValue)
            {
                var vendedor = await _context.Clientes
                    .FirstOrDefaultAsync(c => c.Id == script.VendedorId && c.TipoCliente == "VENDEDOR");
                if (vendedor == null)
                {
                    // Si no es VENDEDOR, buscar como cliente normal
                    vendedor = await _context.Clientes.FindAsync(script.VendedorId);
                    if (vendedor == null)
                        return BadRequest(new { message = "Vendedor no válido" });
                }
            }

            script.FechaCreacion = DateTime.Now;

            _context.ScriptsLlamada.Add(script);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetScript), new { id = script.Id }, script);
        }

        // 10. OBTENER SCRIPTS
        [HttpGet("scripts")]
        public async Task<ActionResult<IEnumerable<object>>> GetScripts(
            [FromQuery] int? vendedorId = null,
            [FromQuery] bool? activo = null)
        {
            var query = _context.ScriptsLlamada
                .Include(s => s.Vendedor)
                .AsQueryable();

            if (vendedorId.HasValue)
                query = query.Where(s => s.VendedorId == vendedorId.Value || s.VendedorId == null);

            if (activo.HasValue)
                query = query.Where(s => s.Activo == activo.Value);

            var scripts = await query
                .OrderBy(s => s.OrdenEjecucion)
                .Select(s => new
                {
                    s.Id,
                    s.VendedorId,
                    Vendedor = s.Vendedor != null ? new { s.Vendedor.Id, s.Vendedor.Nombre } : null,
                    s.Nombre,
                    s.Descripcion,
                    s.HorasDespuesActivacion,
                    s.OrdenEjecucion,
                    s.Activo,
                    s.IntentosPermitidos,
                    Horario = $"{s.HorarioInicio:hh\\:mm} - {s.HorarioFin:hh\\:mm}",
                    s.FechaCreacion
                })
                .ToListAsync();

            return Ok(scripts);
        }

        // 11. OBTENER UN SCRIPT
        [HttpGet("scripts/{id}")]
        public async Task<ActionResult<ScriptLlamada>> GetScript(int id)
        {
            var script = await _context.ScriptsLlamada
                .Include(s => s.Vendedor)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (script == null)
                return NotFound(new { message = "Script no encontrado" });

            return Ok(script);
        }

        // 12. ACTUALIZAR SCRIPT
        [HttpPut("scripts/{id}")]
        public async Task<IActionResult> ActualizarScript(int id, [FromBody] ScriptLlamada script)
        {
            if (id != script.Id)
                return BadRequest(new { message = "ID no coincide" });

            var scriptExistente = await _context.ScriptsLlamada.FindAsync(id);
            if (scriptExistente == null)
                return NotFound(new { message = "Script no encontrado" });

            // Actualizar campos
            scriptExistente.Nombre = script.Nombre;
            scriptExistente.Descripcion = script.Descripcion;
            scriptExistente.HorasDespuesActivacion = script.HorasDespuesActivacion;
            scriptExistente.ScriptJson = script.ScriptJson;
            scriptExistente.Activo = script.Activo;
            scriptExistente.IntentosPermitidos = script.IntentosPermitidos;
            scriptExistente.HorarioInicio = script.HorarioInicio;
            scriptExistente.HorarioFin = script.HorarioFin;
            scriptExistente.OrdenEjecucion = script.OrdenEjecucion;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Script actualizado exitosamente" });
        }

        // 13. ELIMINAR SCRIPT
        [HttpDelete("scripts/{id}")]
        public async Task<IActionResult> EliminarScript(int id)
        {
            var script = await _context.ScriptsLlamada.FindAsync(id);
            if (script == null)
                return NotFound(new { message = "Script no encontrado" });

            _context.ScriptsLlamada.Remove(script);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Script eliminado exitosamente" });
        }

        // 14. OBTENER RESPUESTAS DE UNA LLAMADA
        [HttpGet("respuestas/{llamadaId}")]
        public async Task<ActionResult<IEnumerable<RespuestaIA>>> GetRespuestas(int llamadaId)
        {
            var respuestas = await _context.RespuestasIA
                .Where(r => r.LlamadaId == llamadaId)
                .OrderBy(r => r.FechaRegistro)
                .ToListAsync();

            return Ok(respuestas);
        }

        // 15. REPORTE DE SEGUIMIENTOS REQUERIDOS
        [HttpGet("seguimientos-pendientes")]
        public async Task<ActionResult<IEnumerable<object>>> GetSeguimientosPendientes(
            [FromQuery] int? vendedorId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.RespuestasIA
                .Include(r => r.Llamada)
                    .ThenInclude(l => l.ClienteFinal)
                .Include(r => r.Llamada)
                    .ThenInclude(l => l.Vendedor)
                .Where(r => r.RequiereSeguimiento);

            if (vendedorId.HasValue)
                query = query.Where(r => r.Llamada.VendedorId == vendedorId.Value);

            var total = await query.CountAsync();

            var seguimientos = await query
                .OrderByDescending(r => r.FechaRegistro)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    RespuestaId = r.Id,
                    LlamadaId = r.LlamadaId,
                    ClienteFinal = new
                    {
                        r.Llamada.ClienteFinal.Id,
                        r.Llamada.ClienteFinal.Nombre,
                        r.Llamada.ClienteFinal.Celular,
                        r.Llamada.ClienteFinal.Email
                    },
                    Vendedor = new
                    {
                        r.Llamada.Vendedor.Id,
                        r.Llamada.Vendedor.Nombre
                    },
                    Pregunta = r.PreguntaTexto,
                    Respuesta = r.RespuestaCliente,
                    Categoria = r.CategoriaRespuesta,
                    Sentimiento = r.Sentimiento,
                    Tags = r.Tags,
                    FechaRespuesta = r.FechaRegistro,
                    Llamada = new
                    {
                        r.Llamada.FechaEjecucion,
                        r.Llamada.Resultado,
                        r.Llamada.GrabacionUrl
                    }
                })
                .ToListAsync();

            return Ok(new
            {
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Data = seguimientos
            });
        }

        // 16. MARCAR SEGUIMIENTO COMO RESUELTO
        [HttpPut("seguimientos/{id}/resolver")]
        public async Task<IActionResult> ResolverSeguimiento(int id)
        {
            var respuesta = await _context.RespuestasIA.FindAsync(id);
            if (respuesta == null)
                return NotFound(new { message = "Respuesta no encontrada" });

            respuesta.RequiereSeguimiento = false;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Seguimiento marcado como resuelto" });
        }

        // 17. AGREGAR NOTA A UNA LLAMADA
        [HttpPost("{id}/nota")]
        public async Task<IActionResult> AgregarNotaLlamada(int id, [FromBody] AgregarNotaRequest request)
        {
            var llamada = await _context.LlamadasIA.FindAsync(id);
            if (llamada == null)
                return NotFound(new { message = "Llamada no encontrada" });

            // Podrías agregar un campo "Notas" al modelo LlamadaIA o crear una tabla aparte
            // Por ahora, actualizamos la transcripción
            llamada.TranscripcionCompleta += $"\n\n[NOTA: {DateTime.Now}] {request.Nota}";
            llamada.FechaActualizacion = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Nota agregada exitosamente" });
        }

        // MÉTODO DE EJEMPLO PARA LA LLAMADA REAL (requiere Twilio)
        [HttpPost("ejecutar/{id}")]
        public async Task<ActionResult<object>> EjecutarLlamada(int id)
        {
            try
            {
                var llamada = await _context.LlamadasIA
                    .Include(l => l.ClienteFinal)
                    .Include(l => l.Vendedor)
                    .FirstOrDefaultAsync(l => l.Id == id);

                if (llamada == null)
                    return NotFound(new { message = "Llamada no encontrada" });

                // Aquí iría la lógica con Twilio
                // Por ahora, simulamos una llamada exitosa

                llamada.Estado = "COMPLETADA";
                llamada.FechaEjecucion = DateTime.Now;
                llamada.Resultado = "CONTESTO";
                llamada.DuracionSegundos = 120;
                llamada.Sentimiento = "POSITIVO";
                llamada.Satisfaccion = 8;
                llamada.FechaActualizacion = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Llamada ejecutada exitosamente (simulación)",
                    llamadaId = llamada.Id,
                    estado = llamada.Estado,
                    resultado = llamada.Resultado
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error ejecutando llamada ID: {id}");
                return StatusCode(500, new { message = "Error ejecutando llamada", error = ex.Message });
            }
        }

        // 18. BUSCAR LLAMADAS POR TELÉFONO
        [HttpGet("buscar")]
        public async Task<ActionResult<IEnumerable<object>>> BuscarLlamadas(
            [FromQuery] string telefono,
            [FromQuery] int? vendedorId = null)
        {
            var query = _context.LlamadasIA
                .Include(l => l.ClienteFinal)
                .Include(l => l.Vendedor)
                .Where(l => l.ClienteFinal.Celular != null && l.ClienteFinal.Celular.Contains(telefono));

            if (vendedorId.HasValue)
                query = query.Where(l => l.VendedorId == vendedorId.Value);

            var llamadas = await query
                .OrderByDescending(l => l.FechaCreacion)
                .Take(50)
                .Select(l => new
                {
                    l.Id,
                    l.ActivacionId,
                    Vendedor = new { l.Vendedor.Id, l.Vendedor.Nombre },
                    ClienteFinal = new { l.ClienteFinal.Id, l.ClienteFinal.Nombre, l.ClienteFinal.Celular },
                    l.Estado,
                    l.Resultado,
                    l.Sentimiento,
                    l.FechaProgramada,
                    l.FechaEjecucion,
                    l.DuracionSegundos,
                    l.FechaCreacion
                })
                .ToListAsync();

            return Ok(new
            {
                Total = llamadas.Count,
                TelefonoBuscado = telefono,
                Data = llamadas
            });
        }

        // 19. EXPORTAR LLAMADAS A CSV
        [HttpGet("exportar-csv")]
        public async Task<IActionResult> ExportarCsv(
            [FromQuery] int? vendedorId = null,
            [FromQuery] DateTime? fechaDesde = null,
            [FromQuery] DateTime? fechaHasta = null)
        {
            var query = _context.LlamadasIA
                .Include(l => l.ClienteFinal)
                .Include(l => l.Vendedor)
                .AsQueryable();

            if (vendedorId.HasValue)
                query = query.Where(l => l.VendedorId == vendedorId.Value);

            if (fechaDesde.HasValue)
                query = query.Where(l => l.FechaCreacion >= fechaDesde.Value);

            if (fechaHasta.HasValue)
                query = query.Where(l => l.FechaCreacion <= fechaHasta.Value);

            var llamadas = await query
                .OrderByDescending(l => l.FechaCreacion)
                .Select(l => new
                {
                    l.Id,
                    Vendedor = l.Vendedor.Nombre,
                    Cliente = l.ClienteFinal.Nombre,
                    Telefono = l.ClienteFinal.Celular,
                    l.Estado,
                    l.Resultado,
                    l.Sentimiento,
                    l.Satisfaccion,
                    FechaProgramada = l.FechaProgramada.ToString("yyyy-MM-dd HH:mm"),
                    FechaEjecucion = l.FechaEjecucion.HasValue ? l.FechaEjecucion.Value.ToString("yyyy-MM-dd HH:mm") : "",
                    l.DuracionSegundos,
                    l.IntentoNumero,
                    FechaCreacion = l.FechaCreacion.ToString("yyyy-MM-dd HH:mm")
                })
                .ToListAsync();

            // Generar CSV
            var csv = "ID,Vendedor,Cliente,Telefono,Estado,Resultado,Sentimiento,Satisfaccion,FechaProgramada,FechaEjecucion,DuracionSegundos,Intento,FechaCreacion\n";

            foreach (var llamada in llamadas)
            {
                csv += $"\"{llamada.Id}\",";
                csv += $"\"{llamada.Vendedor?.Replace("\"", "\"\"")}\",";
                csv += $"\"{llamada.Cliente?.Replace("\"", "\"\"")}\",";
                csv += $"\"{llamada.Telefono}\",";
                csv += $"\"{llamada.Estado}\",";
                csv += $"\"{llamada.Resultado}\",";
                csv += $"\"{llamada.Sentimiento}\",";
                csv += $"\"{llamada.Satisfaccion}\",";
                csv += $"\"{llamada.FechaProgramada}\",";
                csv += $"\"{llamada.FechaEjecucion}\",";
                csv += $"\"{llamada.DuracionSegundos}\",";
                csv += $"\"{llamada.IntentoNumero}\",";
                csv += $"\"{llamada.FechaCreacion}\"\n";
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            var result = new FileContentResult(bytes, "text/csv")
            {
                FileDownloadName = $"llamadas_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            return result;
        }
        private DateTime GetNowForDatabase()
        {
            return DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc);
        }
        // Añade este método después del último endpoint existente en tu controller
        [HttpPost("generar-datos-prueba")]
        public async Task<ActionResult<object>> GenerarDatosPrueba()
        {
            try
            {
                _logger.LogInformation("Generando datos de prueba para Call Center...");

                var ahora = DateTime.Now; // Helper para fechas

                // 1. VERIFICAR SI YA HAY LLAMADAS
                if (await _context.LlamadasIA.AnyAsync())
                {
                    return Ok(new
                    {
                        message = "Ya existen datos en el sistema",
                        llamadasExistentes = await _context.LlamadasIA.CountAsync(),
                        sugerencia = "Usa /api/callcenter para ver los datos existentes"
                    });
                }

                // 2. CREAR O USAR VENDEDOR DE PRUEBA
                var vendedor = await _context.Clientes
                    .Where(c => c.TipoCliente == "VENDEDOR")
                    .FirstOrDefaultAsync();

                if (vendedor == null)
                {
                    var guid = Guid.NewGuid().ToString().Substring(0, 8);
                    vendedor = new Cliente
                    {
                        Nombre = "Juan Pérez - Vendedor",
                        Usuario = $"vendedor_demo_{guid}",
                        Celular = "+51987654321",
                        Email = $"vendedor_{guid}@distels.com",
                        TipoCliente = "VENDEDOR",
                        Estado = "activo",
                        FechaCreacion = ahora
                    };
                    _context.Clientes.Add(vendedor);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Vendedor de prueba creado: {Nombre}", vendedor.Nombre);
                }

                // 3. CREAR CLIENTES FINALES DE PRUEBA
                var nombresClientes = new[]
                {
            "Carlos Rodríguez", "María García", "Luis Fernández", "Ana Martínez",
            "Pedro López", "Laura González", "Javier Sánchez", "Sofía Ramírez",
            "Miguel Torres", "Isabel Díaz"
        };

                var telefonosClientes = new[]
                {
            "+51911111111", "+51922222222", "+51933333333", "+51944444444",
            "+51955555555", "+51966666666", "+51977777777", "+51988888888",
            "+51999999999", "+51900000000"
        };

                var clientesFinales = new List<Cliente>();
                for (int i = 0; i < 10; i++)
                {
                    var guid = Guid.NewGuid().ToString().Substring(0, 8);
                    var cliente = new Cliente
                    {
                        Nombre = nombresClientes[i],
                        Usuario = $"cliente{i + 1}_{guid}",
                        Contrasena = "demo123",
                        Celular = telefonosClientes[i],
                        Email = $"cliente{i + 1}_{guid}@demo.com",
                        TipoCliente = "FINAL",
                        Estado = "activo",
                        FechaCreacion = ahora.AddDays(-(i + 1))
                    };
                    clientesFinales.Add(cliente);
                }

                _context.Clientes.AddRange(clientesFinales);
                await _context.SaveChangesAsync();
                _logger.LogInformation("10 clientes finales creados");

                // 4. CREAR ACTIVACIONES DE PRUEBA (para cada cliente)
                var activaciones = new List<Activacion>();
                foreach (var cliente in clientesFinales)
                {
                    var tarjeta = await _context.Tarjetas.FirstOrDefaultAsync(); // Usamos la primera tarjeta disponible
                    if (tarjeta == null) continue;

                    var activacion = new Activacion
                    {
                        IdClienteFinal = cliente.Id,
                        IdTarjeta = tarjeta.IdTarjeta,
                        UsuarioEnviado = cliente.Usuario,
                        ContrasenaEnviada = cliente.Contrasena,
                        FechaActivacion = ahora
                    };
                    activaciones.Add(activacion);
                }

                _context.Activaciones.AddRange(activaciones);
                await _context.SaveChangesAsync();
                _logger.LogInformation("{0} activaciones creadas", activaciones.Count);

                // 5. CREAR LLAMADAS DE PRUEBA
                var llamadas = new List<LlamadaIA>();
                var random = new Random();

                for (int i = 0; i < 20; i++)
                {
                    var clienteIndex = random.Next(clientesFinales.Count);
                    var activacionIndex = random.Next(activaciones.Count);

                    var estadoRandom = random.Next(100);
                    string estado = estadoRandom < 40 ? "COMPLETADA" :
                                    estadoRandom < 60 ? "FALLIDA" :
                                    estadoRandom < 80 ? "PROGRAMADA" : "EN_CURSO";

                    var fechaCreacion = ahora.AddDays(-random.Next(30));

                    var llamada = new LlamadaIA
                    {
                        VendedorId = vendedor.Id,
                        ClienteFinalId = clientesFinales[clienteIndex].Id,
                        ActivacionId = activaciones[activacionIndex].IdActivacion,
                        TipoLlamada = "SALIENTE",
                        Estado = estado,
                        HorasDespuesActivacion = 24,
                        FechaProgramada = fechaCreacion.AddHours(24),
                        FechaEjecucion = estado == "PROGRAMADA" ? null : fechaCreacion.AddHours(24).AddMinutes(random.Next(5, 60)),
                        Resultado = estado == "PROGRAMADA" ? null :
                                   estado == "COMPLETADA" ? "CONTESTO" :
                                   estado == "FALLIDA" ? "NO_CONTESTO" : "OCUPADO",
                        DuracionSegundos = estado == "PROGRAMADA" ? null : random.Next(45, 300),
                        Sentimiento = estado == "COMPLETADA" ?
                            (random.Next(100) < 80 ? "POSITIVO" : "NEUTRAL") :
                            estado == "FALLIDA" ? "NEGATIVO" : null,
                        Satisfaccion = estado == "COMPLETADA" ? random.Next(7, 10) :
                                      estado == "FALLIDA" ? random.Next(1, 4) : null,
                        IntentoNumero = estado == "PROGRAMADA" ? 0 : 1,
                        MaxIntentos = 3,
                        FechaCreacion = fechaCreacion,
                        FechaActualizacion = ahora
                    };

                    llamadas.Add(llamada);
                }

                _context.LlamadasIA.AddRange(llamadas);
                await _context.SaveChangesAsync();
                _logger.LogInformation("20 llamadas de prueba creadas");

                // 6. CREAR RESPUESTAS DE IA PARA LLAMADAS COMPLETADAS
                var respuestas = new List<RespuestaIA>();
                foreach (var llamada in llamadas.Where(l => l.Estado == "COMPLETADA").Take(5))
                {
                    respuestas.Add(new RespuestaIA
                    {
                        LlamadaId = llamada.Id,
                        PreguntaId = "p1",
                        PreguntaTexto = "¿Recibiste las credenciales correctamente?",
                        RespuestaCliente = "Sí, todo perfecto",
                        CategoriaRespuesta = "CONFIRMACION",
                        Sentimiento = "POSITIVO",
                        RequiereSeguimiento = false,
                        FechaRegistro = llamada.FechaEjecucion!.Value
                    });

                    respuestas.Add(new RespuestaIA
                    {
                        LlamadaId = llamada.Id,
                        PreguntaId = "p2",
                        PreguntaTexto = "¿Hay algo en lo que podamos ayudarte?",
                        RespuestaCliente = "No, todo está bien por ahora",
                        CategoriaRespuesta = "SIN_REQUERIMIENTOS",
                        Sentimiento = "POSITIVO",
                        RequiereSeguimiento = false,
                        FechaRegistro = llamada.FechaEjecucion!.Value.AddSeconds(30)
                    });
                }

                _context.RespuestasIA.AddRange(respuestas);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Respuestas de IA creadas");

                return Ok(new
                {
                    success = true,
                    message = "✅ Datos de prueba generados exitosamente",
                    resumen = new
                    {
                        vendedorCreado = new { vendedor.Id, vendedor.Nombre, vendedor.Celular },
                        clientesCreados = clientesFinales.Count,
                        activacionesCreadas = activaciones.Count,
                        llamadasCreadas = llamadas.Count,
                        respuestasCreadas = respuestas.Count,
                        distribucionEstados = llamadas.GroupBy(l => l.Estado)
                                                      .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generando datos de prueba");
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message,
                    innerError = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }


        }
        // 20. EJECUTAR LLAMADAS MASIVAS (Programadas -> En Curso)
        [HttpPost("ejecutar-masivo")]
        public async Task<ActionResult> EjecutarLlamadasMasivo([FromBody] EjecutarMasivoRequest request)
        {
            try
            {
                _logger.LogInformation($"Ejecutando llamadas masivas - Cantidad: {request.Cantidad}");

                // 1. Obtener llamadas PROGRAMADAS
                var ahora = DateTime.UtcNow;
                var llamadasPendientes = await _context.LlamadasIA
                    .Where(l => l.Estado == "PROGRAMADA" && l.FechaProgramada <= ahora)
                    .Take(request.Cantidad)
                    .Include(l => l.ClienteFinal)
                    .Include(l => l.Vendedor)
                    .ToListAsync();

                if (!llamadasPendientes.Any())
                    return Ok(new
                    {
                        message = "No hay llamadas programadas para ejecutar",
                        total = 0,
                        exitosas = 0,
                        fallidas = 0
                    });

                var resultados = new List<object>();
                var exitosas = 0;
                var fallidas = 0;

                // 2. Procesar cada llamada
                foreach (var llamada in llamadasPendientes)
                {
                    try
                    {
                        if (llamada.ClienteFinal == null)
                        {
                            resultados.Add(new
                            {
                                llamadaId = llamada.Id,
                                error = "Cliente no encontrado",
                                success = false
                            });
                            fallidas++;
                            continue;
                        }

                        // 3. Aquí iría la lógica REAL con Twilio
                        // Por ahora, simulamos la ejecución

                        // SIMULACIÓN: 80% de éxito, 20% de falla
                        var random = new Random();
                        var exito = random.Next(100) < 80;

                        if (exito)
                        {
                            llamada.Estado = "EN_CURSO";
                            llamada.FechaEjecucion = ahora;
                            llamada.FechaActualizacion = ahora;

                            resultados.Add(new
                            {
                                llamadaId = llamada.Id,
                                cliente = llamada.ClienteFinal.Nombre,
                                telefono = llamada.ClienteFinal.Celular,
                                estado = "EN_CURSO",
                                mensaje = "Llamada iniciada (simulación)",
                                success = true
                            });
                            exitosas++;
                        }
                        else
                        {
                            llamada.Estado = "FALLIDA";
                            llamada.Resultado = "SIMULACION_FALLIDA";
                            llamada.FechaActualizacion = ahora;
                            llamada.IntentoNumero = llamada.IntentoNumero + 1;

                            resultados.Add(new
                            {
                                llamadaId = llamada.Id,
                                cliente = llamada.ClienteFinal.Nombre,
                                error = "Simulación de fallo",
                                success = false
                            });
                            fallidas++;
                        }
                    }
                    catch (Exception ex)
                    {
                        llamada.Estado = "FALLIDA";
                        llamada.Resultado = "ERROR_SISTEMA";
                        llamada.FechaActualizacion = ahora;

                        resultados.Add(new
                        {
                            llamadaId = llamada.Id,
                            error = ex.Message,
                            success = false
                        });
                        fallidas++;
                    }
                }

                // 4. Guardar cambios
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Procesadas {resultados.Count} llamadas",
                    total = resultados.Count,
                    exitosas = exitosas,
                    fallidas = fallidas,
                    detalle = resultados
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ejecutar-masivo");
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        // 21. INICIAR LLAMADA REAL (para integración con Twilio)
        [HttpPost("iniciar-llamada-real")]
        public async Task<IActionResult> IniciarLlamadaReal([FromBody] IniciarLlamadaRealRequest request)
        {
            try
            {
                // 1. Buscar cliente
                var cliente = await _context.Clientes
                    .FirstOrDefaultAsync(c => c.Id == request.ClienteId);

                if (cliente == null)
                    return NotFound(new { error = "Cliente no encontrado" });

                // 2. Buscar vendedor
                var vendedor = await _context.Clientes
                    .FirstOrDefaultAsync(c => c.Id == request.VendedorId && c.TipoCliente == "VENDEDOR");

                if (vendedor == null)
                    return BadRequest(new { error = "Vendedor no válido" });
                var activacionId = await ObtenerActivacionIdValido(request.ClienteId);
                // 3. Crear registro en BD
                var llamada = new LlamadaIA
                {
                    ActivacionId = activacionId, 
                    VendedorId = request.VendedorId,
                    ClienteFinalId = request.ClienteId,
                    TipoLlamada = "SALIENTE",
                    Estado = "EN_CURSO",
                    TelefonoDestino = cliente.Celular,
                    // ScriptId = request.ScriptId, // Descomenta cuando tengas ScriptId en tu modelo
                    FechaProgramada = DateTime.Now,
                    FechaEjecucion = DateTime.Now,
                    FechaCreacion = DateTime.Now,
                    FechaActualizacion = DateTime.Now
                };

                _context.LlamadasIA.Add(llamada);
                await _context.SaveChangesAsync();

                // 4. Generar ID de Twilio simulado (formato correcto)
                var random = new Random();
                var twilioCallId = $"CA{random.Next(1000000000, int.MaxValue).ToString("D10")}";

                return Ok(new
                {
                    success = true,
                    message = "✅ Llamada real iniciada (simulación)",
                    data = new
                    {
                        llamadaId = llamada.Id,
                        twilioCallId = twilioCallId, // Ahora es string
                        cliente = new { cliente.Id, cliente.Nombre, cliente.Celular },
                        vendedor = new { vendedor.Id, vendedor.Nombre },
                        estado = llamada.Estado,
                        fechaInicio = llamada.FechaEjecucion,
                        telefonoDestino = llamada.TelefonoDestino,
                        notas = "Para llamadas REALES, integra con Twilio. Usa /api/callcenter/prueba-twilio para test."
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error iniciando llamada real");
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message,
                    detalle = "Verifica los datos del cliente y vendedor"
                });
            }
        }
        private async Task<int> ObtenerActivacionIdValido(int clienteId)
        {
            // Buscar cualquier activación existente
            var activacion = await _context.Activaciones
                .FirstOrDefaultAsync(a => a.IdClienteFinal == clienteId);

            if (activacion != null)
                return activacion.IdActivacion;

            // Si no existe, usar la primera activación de la base de datos
            var primeraActivacion = await _context.Activaciones
                .OrderBy(a => a.IdActivacion)
                .FirstOrDefaultAsync();

            if (primeraActivacion != null)
                return primeraActivacion.IdActivacion;

            // Si no hay ninguna activación, crear una
            var tarjeta = await _context.Tarjetas.FirstOrDefaultAsync();
            if (tarjeta == null)
            {
                throw new Exception("No hay tarjetas ni activaciones en el sistema");
            }

            var nuevaActivacion = new Activacion
            {
                IdClienteFinal = clienteId,
                IdTarjeta = tarjeta.IdTarjeta,
                UsuarioEnviado = "temp",
                ContrasenaEnviada = "temp123",
                FechaActivacion = DateTime.UtcNow
            };

            _context.Activaciones.Add(nuevaActivacion);
            await _context.SaveChangesAsync();

            return nuevaActivacion.IdActivacion;
        }
        // 22. PROBAR TWILIO (sin costo - números de prueba)
        [HttpPost("prueba-twilio")]
        public async Task<IActionResult> PruebaTwilioGratis()
        {
            try
            {
                // Esta es una prueba SIN COSTO usando números de prueba de Twilio
                var numeroPrueba = "+15005550006"; // Número que SIEMPRE contesta (GRATIS)

                return Ok(new
                {
                    success = true,
                    message = "✅ Endpoint de prueba Twilio listo",
                    instrucciones = new[]
                    {
                "1. Registrate en twilio.com (obtienes $15 gratis)",
                "2. Instala NuGet: Install-Package Twilio",
                "3. Configura credenciales en appsettings.json",
                "4. Usa el endpoint /iniciar-llamada-real con datos reales",
                "5. Los números de prueba NO tienen costo: +15005550001 al +15005550009"
            },
                    ejemploCodigo = @"// Código para llamada REAL:
var call = await CallResource.CreateAsync(
    to: new PhoneNumber('" + numeroPrueba + @"'),
    from: new PhoneNumber('+15005550000'),
    twiml: new Twilio.Types.Twiml(@'<Response>
        <Say>Hola, prueba de DISTELS</Say>
        <Hangup/>
    </Response>')
);",
                    siguientePaso = "Configura Twilio y usa /iniciar-llamada-real con un cliente real"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    error = ex.Message,
                    solucion = "Instala el paquete Twilio: Install-Package Twilio"
                });
            }
        }

        // 23. SIMULAR EJECUCIÓN MASIVA (para pruebas)
        [HttpPost("simular-masivo")]
        public async Task<ActionResult> SimularEjecucionMasiva([FromBody] EjecutarMasivoRequest request)
        {
            // Este método solo simula, no necesita Twilio configurado
            try
            {
                var resultados = new List<object>();
                var ahora = DateTime.UtcNow;

                // Crear algunas llamadas programadas de prueba
                for (int i = 0; i < request.Cantidad; i++)
                {
                    var random = new Random();
                    var exito = random.Next(100) < 70; // 70% de éxito

                    resultados.Add(new
                    {
                        llamadaId = i + 1,
                        cliente = $"Cliente Simulado {i + 1}",
                        telefono = $"+519{random.Next(10000000, 99999999)}",
                        estado = exito ? "EN_CURSO" : "FALLIDA",
                        mensaje = exito ? "Llamada simulada exitosa" : "Llamada simulada fallida",
                        esSimulacion = true,
                        success = exito
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = $"Simulación de {request.Cantidad} llamadas ejecutadas",
                    total = request.Cantidad,
                    exitosas = resultados.Count(r => ((dynamic)r).success == true),
                    fallidas = resultados.Count(r => ((dynamic)r).success == false),
                    nota = "Esta es una SIMULACIÓN. Para llamadas REALES, configura Twilio.",
                    detalle = resultados
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en simular-masivo");
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }


    }
    // CLASES DE REQUEST
    public class ProgramarLlamadaRequest
    {
        public int ActivacionId { get; set; }
        public int HorasDespuesActivacion { get; set; } = 24;
    }

    public class ReprogramarLlamadaRequest
    {
        public DateTime NuevaFecha { get; set; }
    }

    public class AgregarNotaRequest
    {
        public string Nota { get; set; } = null!;
    }
    // Agrega esta clase al final del archivo (antes del último })
    public class EjecutarMasivoRequest
    {
        public int Cantidad { get; set; } = 10;
        public bool Forzar { get; set; } = false;
    }

    public class IniciarLlamadaRealRequest
    {
        public int ClienteId { get; set; }
        public int VendedorId { get; set; }
        public int? ScriptId { get; set; }
        public bool ModoInteractivo { get; set; } = false;
    }
}