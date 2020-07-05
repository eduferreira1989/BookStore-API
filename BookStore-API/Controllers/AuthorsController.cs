using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using BookStore_API.Contracts;
using BookStore_API.Data;
using BookStore_API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookStore_API.Controllers
{
    /// <summary>
    /// Endpoint to interact with the Authors in book store's database
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public class AuthorsController : ControllerBase
    {
        private readonly IAuthorRepository _authorRepository;
        private readonly ILoggerService _logger;
        private readonly IMapper _mapper;

        public AuthorsController(IAuthorRepository authorRepository, ILoggerService logger, IMapper mapper)
        {
            _authorRepository = authorRepository;
            _logger = logger;
            _mapper = mapper;
        }

        /// <summary>
        /// Gets all Authors
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAuthors()
        {
            try
            {
                var authors = await _authorRepository.FindAll();
                var response = _mapper.Map<IList<AuthorDTO>>(authors);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return InternalError(ex);
            }
        }

        /// <summary>
        /// Gets an author by Id.
        /// </summary>
        /// <param name="id">Id</param>
        /// <returns></returns>
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAuthor(int id)
        {
            try
            {
                var author = await _authorRepository.FindById(id);

                if (author == null)
                {
                    _logger.LogWarn($"Author not found - Id: {id}");
                    return NotFound();
                }

                var response = _mapper.Map<AuthorDTO>(author);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return InternalError(ex);
            }
        }

        /// <summary>
        /// Creates an Author
        /// </summary>
        /// <param name="author"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] AuthorCreateDTO authorDTO)
        {
            try
            {
                if (authorDTO == null)
                {
                    _logger.LogWarn("Empty Author was submitted.");
                    return BadRequest(ModelState);
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarn("Author data was incomplete.");
                    return BadRequest(ModelState);
                }

                var author = _mapper.Map<Author>(authorDTO);
                var isSuccess = await _authorRepository.Create(author);
                if (!isSuccess)
                    return InternalError("Author creation failed.");

                return Created("Create", new { author });
            }
            catch (Exception ex)
            {
                return InternalError(ex);
            }
        }

        /// <summary>
        /// Updates an Author
        /// </summary>
        /// <param name="id"></param>
        /// <param name="author"></param>
        /// <returns></returns>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Administrator, Customer")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(int id, [FromBody] AuthorUpdateDTO authorDTO)
        {
            try
            {
                if (id < 1 || authorDTO == null || id != authorDTO.Id)
                {
                    _logger.LogWarn("Empty Author was submitted or id is invalid.");
                    return BadRequest(ModelState);
                }

                var isExists = await _authorRepository.IsExists(id);
                if (!isExists)
                {
                    _logger.LogWarn("Author was not found.");
                    return NotFound();
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarn("Author data was incomplete.");
                    return BadRequest(ModelState);
                }

                var author = _mapper.Map<Author>(authorDTO);
                var isSuccess = await _authorRepository.Update(author);
                if (!isSuccess)
                {
                    return InternalError("Author update failed.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return InternalError(ex);
            }
        }

        /// <summary>
        /// Deletes an Author
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Customer")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id < 1)
                {
                    _logger.LogWarn("Invalid Author Id.");
                    return BadRequest();
                }

                var isExists = await _authorRepository.IsExists(id);
                if (!isExists)
                {
                    _logger.LogWarn("Author was not found.");
                    return NotFound();
                }

                var author = await _authorRepository.FindById(id);
                var isSuccess = await _authorRepository.Delete(author);
                if (!isSuccess)
                {
                    return InternalError("Author delete failed");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return InternalError(ex);
            }
        }

        private ObjectResult InternalError(Exception ex)
        {
            return InternalError($"Error when getting authors:\n{ex.Message}\n\nMore details in Inner exception:\n{ex.InnerException}");
        }

        private ObjectResult InternalError(string message)
        {
            _logger.LogError(message);
            return StatusCode(500, "Something went wrong. Please contact the Administrator.");
        }
    }
}
