using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GastronomePlatform.WebAPI.Controllers.Dishes
{
    /// <summary>
    /// Контроллер модуля Dishes — каталог блюд и рецептов.
    /// Предоставляет эндпоинты для получения и обновления блюд.
    /// </summary>
    [ApiController]
    [Route("api/dishes")]
    public sealed class DishesController : ApiController
    {
        #region Request Models

        #endregion

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="DishesController"/>.
        /// </summary>
        /// <param name="sender">Отправитель команд MediatR.</param>
        public DishesController(ISender sender) : base(sender) { }

        #region GET Endpoints

        #endregion

        #region PUT Endpoints

        #endregion

        #region POST Endpoints

        #endregion

        #region DELETE Endpoints

        #endregion
    }
}
