using EatTogether.API.Models.Infra;
using EatTogether.Models.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EatTogether.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class SetMealsController : ControllerBase
{
    private readonly ISetMealRepository _setMealRepository;
	private readonly ImageUrlResolver _imageUrlResolver;

    public SetMealsController(ISetMealRepository setMealRepository, ImageUrlResolver imageUrlResolver)
    {
        _setMealRepository = setMealRepository;
		_imageUrlResolver = imageUrlResolver;
    }

    // GET /api/SetMeals/active
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var meals = await _setMealRepository.GetAllActiveAsync();
        var result = meals.Select(s => new
        {
            id            = s.Id,
            setMealName   = s.SetMealName,
            description   = s.Description,
            setPrice      = s.SetPrice,
            imageUrl      = _imageUrlResolver.Resolve(s.ImageUrl, s.SetMealName, "setmeals"),
            isRecommended = s.IsRecommended,
            isPopular     = s.IsPopular,
            startTime     = s.StartTime,
            endTime       = s.EndTime,
            items = s.Items
                .OrderBy(i => i.DisplayOrder)
                .Select(i => new
                {
                    dishId        = i.DishId,
                    dishName      = i.DishName,
                    dishPrice     = i.DishPrice,
                    quantity      = i.Quantity,
                    isOptional    = i.IsOptional,
                    optionGroupNo = i.OptionGroupNo,
                    pickLimit     = i.PickLimit,
                    categoryName  = i.CategoryName
                })
        }).ToList();
        return Ok(result);
    }

    // GET /api/SetMeals/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var s = await _setMealRepository.GetByIdAsync(id);
        if (s == null) return NotFound();
        return Ok(new
        {
            id            = s.Id,
            setMealName   = s.SetMealName,
            description   = s.Description,
            setPrice      = s.SetPrice,
            imageUrl      = _imageUrlResolver.Resolve(s.ImageUrl, s.SetMealName, "setmeals"),
            isRecommended = s.IsRecommended,
            isPopular     = s.IsPopular,
            startTime     = s.StartTime,
            endTime       = s.EndTime,
            items = s.Items
                .OrderBy(i => i.DisplayOrder)
                .Select(i => new
                {
                    dishId        = i.DishId,
                    dishName      = i.DishName,
                    dishPrice     = i.DishPrice,
                    quantity      = i.Quantity,
                    isOptional    = i.IsOptional,
                    optionGroupNo = i.OptionGroupNo,
                    pickLimit     = i.PickLimit,
                    categoryName  = i.CategoryName
                })
        });
    }
}
