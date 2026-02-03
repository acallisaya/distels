using AutoMapper;
using distels.DTO;
using distels.Models;

namespace distels.Profiles
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // ============================================
            // 1. Mapeos EXISTENTES de parámetros (mantener)
            // ============================================
            CreateMap<Parametro, ParametroReadDTO>()
                .ForMember(dest => dest.idparametro, opt => opt.MapFrom(src => src.cfg_idparametro))
                .ForMember(dest => dest.descparametro, opt => opt.MapFrom(src => src.cfg_descparametro))
                .ForMember(dest => dest.tipoparametro, opt => opt.MapFrom(src => src.cfg_tipoparametro))
                .ForMember(dest => dest.estado, opt => opt.MapFrom(src => src.cfg_estado));

            CreateMap<ParametroReadDTO, Parametro>()
                .ForMember(dest => dest.cfg_idparametro, opt => opt.MapFrom(src => src.idparametro))
                .ForMember(dest => dest.cfg_descparametro, opt => opt.MapFrom(src => src.descparametro))
                .ForMember(dest => dest.cfg_tipoparametro, opt => opt.MapFrom(src => src.tipoparametro))
                .ForMember(dest => dest.cfg_estado, opt => opt.MapFrom(src => src.estado));

            // ============================================
            // 2. NUEVOS MAPEOS para sistema de tarjetas
            // ============================================

            // SERVICIOS
            CreateMap<CrearServicioDTO, Servicio>();
            CreateMap<Servicio, ServicioDTO>();

            // PLANES
            CreateMap<CrearPlanDTO, Plan>();
            CreateMap<Plan, PlanDTO>();

            // TARJETAS - VERSIÓN SIMPLIFICADA (sin relaciones problemáticas)
            CreateMap<Tarjeta, TarjetaDTO>()
                .ForMember(dest => dest.Plan, opt => opt.Ignore())       // ← Ignorar por ahora
                .ForMember(dest => dest.Perfil, opt => opt.Ignore())     // ← Ignorar por ahora
                .ForMember(dest => dest.Vendedor, opt => opt.Ignore())   // ← Ignorar por ahora
                .ForMember(dest => dest.ClienteActivador, opt => opt.Ignore()) // ← Ignorar por ahora
                .ForMember(dest => dest.QRCode, opt => opt.Ignore());

            // CLIENTES
            CreateMap<Cliente, ClienteDTO>();

            // Si necesitas mapeo inverso:
            CreateMap<ClienteDTO, Cliente>()
                .ForMember(dest => dest.Contrasena, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        }
    }
}