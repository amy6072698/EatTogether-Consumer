using EatTogether.API.Models.Infra;
using EatTogether.API.Models.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EatTogether.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DishesController : ControllerBase
    {
        private readonly DishService _dishService;
		private readonly ImageUrlResolver _imageUrlResolver;

        public DishesController(DishService dishService, ImageUrlResolver imageUrlResolver)
        {
            _dishService = dishService;
			_imageUrlResolver = imageUrlResolver;
        }

        [HttpGet("GetAllJson")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllJson()
        {
            var dtos = await _dishService.GetAllAsync();
            return Ok(dtos.Select(d => new
            {
                id = d.Id,
                dishName = d.DishName,
                price = d.Price,
                categoryId = d.CategoryId,
                imageUrl = d.ImageUrl
            }));
        }

        [HttpGet("GetActiveJson")]
        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveJson()
        {
            var dtos = await _dishService.GetAllActiveAsync();

            return Ok(dtos.Select(d => {
                string imageUrl = _imageUrlResolver.Resolve(d.ImageUrl, d.DishName, "dishes");

                return new
                {
                    id = d.Id,
                    dishName = d.DishName,
                    description = d.Description,
                    price = d.Price,
                    categoryId = d.CategoryId,
                    categoryName = d.CategoryName,
                    imageUrl,
                    isRecommended = d.IsRecommended,
                    isPopular = d.IsPopular,
                    isVegetarian = d.IsVegetarian,
                    spicyLevel = d.SpicyLevel,
                    ingredientsJson = d.IngredientsJson,
                    isLimited = d.IsLimited,
                    startDate = d.StartDate,
                    endDate = d.EndDate,
                    averageScore = d.AverageScore,
                    ratingCount = d.RatingCount,
                    stockStatus = d.StockStatus
                };
            }));
        }

        [HttpGet("GetByIdJson")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByIdJson(int id)
        {
            var dto = await _dishService.GetByIdAsync(id);
            if (dto == null) return NotFound();
            return Ok(new
            {
                id = dto.Id,
                dishName = dto.DishName,
                description = dto.Description,
                price = dto.Price,
                categoryId = dto.CategoryId,
                categoryName = dto.CategoryName,
                imageUrl = _imageUrlResolver.Resolve(dto.ImageUrl, dto.DishName, "dishes"),
                isRecommended = dto.IsRecommended,
                isPopular = dto.IsPopular,
                isVegetarian = dto.IsVegetarian,
                spicyLevel = dto.SpicyLevel
            });
        }
    }
}
