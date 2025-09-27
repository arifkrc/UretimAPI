using AutoMapper;
using UretimAPI.DTOs.Product;
using UretimAPI.DTOs.Operation;
using UretimAPI.DTOs.CycleTime;
using UretimAPI.DTOs.ProductionTrackingForm;
using UretimAPI.DTOs.Packing;
using UretimAPI.DTOs.Order;
using UretimAPI.Entities;

namespace UretimAPI.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Product Mappings
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.LastOperationName, opt => opt.MapFrom(src => src.LastOperation.Name))
                .ForMember(dest => dest.LastOperationShortCode, opt => opt.MapFrom(src => src.LastOperation.ShortCode));
            
            CreateMap<CreateProductDto, Product>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.AddedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.LastOperation, opt => opt.Ignore())
                .ForMember(dest => dest.CycleTimes, opt => opt.Ignore())
                .ForMember(dest => dest.ProductionTrackingForms, opt => opt.Ignore())
                .ForMember(dest => dest.Packings, opt => opt.Ignore());

            CreateMap<UpdateProductDto, Product>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.AddedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.LastOperation, opt => opt.Ignore())
                .ForMember(dest => dest.CycleTimes, opt => opt.Ignore())
                .ForMember(dest => dest.ProductionTrackingForms, opt => opt.Ignore())
                .ForMember(dest => dest.Packings, opt => opt.Ignore());

            // Operation Mappings
            CreateMap<Operation, OperationDto>();
            CreateMap<CreateOperationDto, Operation>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.AddedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.ProductsWithLastOperation, opt => opt.Ignore())
                .ForMember(dest => dest.CycleTimes, opt => opt.Ignore());

            CreateMap<UpdateOperationDto, Operation>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.AddedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.ProductsWithLastOperation, opt => opt.Ignore())
                .ForMember(dest => dest.CycleTimes, opt => opt.Ignore());

            // CycleTime Mappings
            CreateMap<CycleTime, CycleTimeDto>()
                .ForMember(dest => dest.OperationName, opt => opt.MapFrom(src => src.Operation.Name))
                .ForMember(dest => dest.OperationShortCode, opt => opt.MapFrom(src => src.Operation.ShortCode))
                .ForMember(dest => dest.ProductCode, opt => opt.MapFrom(src => src.Product.ProductCode))
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name));

            CreateMap<CreateCycleTimeDto, CycleTime>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.AddedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.Operation, opt => opt.Ignore())
                .ForMember(dest => dest.Product, opt => opt.Ignore());

            CreateMap<UpdateCycleTimeDto, CycleTime>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.AddedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.Operation, opt => opt.Ignore())
                .ForMember(dest => dest.Product, opt => opt.Ignore());

            // ProductionTrackingForm Mappings
            CreateMap<ProductionTrackingForm, ProductionTrackingFormDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name));

            CreateMap<CreateProductionTrackingFormDto, ProductionTrackingForm>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.AddedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.Product, opt => opt.Ignore())
                .ForMember(dest => dest.CycleTime, opt => opt.MapFrom(src => src.CycleTime));

            CreateMap<UpdateProductionTrackingFormDto, ProductionTrackingForm>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.AddedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.Product, opt => opt.Ignore())
                .ForMember(dest => dest.CycleTime, opt => opt.MapFrom(src => src.CycleTime));

            // Packing Mappings
            CreateMap<Packing, PackingDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name));

            CreateMap<CreatePackingDto, Packing>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.AddedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.Product, opt => opt.Ignore())
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => 
                    src.Date == default(DateTime) ? UretimAPI.Helpers.DateTimeHelper.Now : src.Date))
                .ForMember(dest => dest.RelatedWithOrder, opt => opt.MapFrom(src => src.RelatedWithOrder));

            CreateMap<UpdatePackingDto, Packing>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.AddedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.Product, opt => opt.Ignore())
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => 
                    src.Date == default(DateTime) ? UretimAPI.Helpers.DateTimeHelper.Now : src.Date))
                .ForMember(dest => dest.RelatedWithOrder, opt => opt.MapFrom(src => src.RelatedWithOrder));

            // Order Mappings
            CreateMap<Order, OrderDto>();
            CreateMap<CreateOrderDto, Order>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.AddedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore());

            CreateMap<UpdateOrderDto, Order>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.DocumentNo, opt => opt.Ignore())
                .ForMember(dest => dest.AddedDateTime, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore());
        }
    }
}