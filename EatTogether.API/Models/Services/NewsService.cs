using EatTogether.API.Models.DTOs;
using EatTogether.API.Models.EfModels;
using EatTogether.API.Models.Infra;
using EatTogether.API.Models.Repositories;

namespace EatTogether.API.Models.Services
{
	public interface INewsService
	{
		Task<NewsPagedResultDto<NewsListDto>> GetNewsListAsync(int page, int pageSize, string? categoryName);
		Task<NewsDetailResultDto?> GetNewsDetailAsync(int id);
		Task<bool> IncrementViewCountAsync(int id);
	}

	public class NewsService : INewsService
	{
		private readonly INewsRepository _newsRepo;
		private readonly ImageUrlResolver _imageUrlResolver;

		public NewsService(INewsRepository newsRepo, ImageUrlResolver imageUrlResolver)
		{
			_newsRepo = newsRepo;
			_imageUrlResolver = imageUrlResolver;
		}

		public async Task<NewsPagedResultDto<NewsListDto>> GetNewsListAsync(int page, int pageSize, string? categoryName)
		{
			int totalCount = await _newsRepo.GetNewsCountAsync(categoryName);
			List<Article> articles = await _newsRepo.GetNewsListAsync(page, pageSize, categoryName);

			List<NewsListDto> newsList = articles.Select(n => new NewsListDto
			{
				Id = n.Id,
				CategoryName = n.Category.Name,
				Title = n.Title,
				Summary = n.Description != null
						  ? (n.Description.Length > 100 ? n.Description.Substring(0, 100) + "…" : n.Description)
						  : "",
				CoverImageUrl = !string.IsNullOrWhiteSpace(n.CoverImageUrl)
								? _imageUrlResolver.Resolve(n.CoverImageUrl, null, "articles")
								: _imageUrlResolver.Resolve("article-14.jpg", null, "articles"), //預設公告圖片
				PublishDate = n.PublishDate,
				IsPinned = n.IsPinned,
				ViewCount = n.ViewCount
			}).ToList();

			return new NewsPagedResultDto<NewsListDto>
			{
				Data = newsList,
				Page = page,
				PageSize = pageSize,
				TotalCount = totalCount,
				TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
			};
		}

		public async Task<NewsDetailResultDto?> GetNewsDetailAsync(int id)
		{
			Article? article = await _newsRepo.GetNewsDetailAsync(id);
			if (article == null) return null;

			NewsDetailDto detail = new NewsDetailDto
			{
				Id = article.Id,
				CategoryName = article.Category.Name,
				Title = article.Title,
				Description = article.Description,
				CoverImageUrl = !string.IsNullOrWhiteSpace(article.CoverImageUrl)
								? _imageUrlResolver.Resolve(article.CoverImageUrl, null, "articles")
								: _imageUrlResolver.Resolve("article-14.jpg", null, "articles"), //預設公告圖片
				PublishDate = article.PublishDate,
				ViewCount = article.ViewCount,
				IsPinned = article.IsPinned
			};

			if (article.PublishDate == null) return null;

			if (article.Status != 1) return null;  //排查文章狀態非發佈中的

			var prev = await _newsRepo.GetPrevArticleAsync(article.PublishDate.Value);
			var next = await _newsRepo.GetNextArticleAsync(article.PublishDate.Value);

			return new NewsDetailResultDto
			{
				Article = detail,
				Prev = prev == null ? null : new NewsNavDto { Id = prev.Id, Title = prev.Title },
				Next = next == null ? null : new NewsNavDto { Id = next.Id, Title = next.Title }
			};
		}

		public async Task<bool> IncrementViewCountAsync(int id)
		{
			int affected = await _newsRepo.IncrementViewCountAsync(id);
			return affected > 0;
		}
	}
}
