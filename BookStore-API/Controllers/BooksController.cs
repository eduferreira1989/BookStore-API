using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using BookStore_API.Contracts;
using BookStore_API.Data;
using BookStore_API.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BookStore_API.Controllers
{
    /// <summary>
    /// Endpoint to interact with the Books in book store's database
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly ILoggerService _logger;
        private readonly IMapper _mapper;
        private readonly IBookRepository _bookRepository;

        public BooksController(ILoggerService logger, IMapper mapper, IBookRepository bookRepository)
        {
            _logger = logger;
            _mapper = mapper;
            _bookRepository = bookRepository;
        }

        /// <summary>
        /// Gets All Books
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetBooks()
        {
            try
            {
                var books = await _bookRepository.FindAll();
                var response = _mapper.Map<IList<BookDTO>>(books);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return InternalError(ex);
            }
        }

        /// <summary>
        /// Gets a Book by Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetBook(int id)
        {
            try
            {
                var book = await _bookRepository.FindById(id);

                if (book == null)
                {
                    _logger.LogWarn($"Book not found - Id: {id}");
                    return NotFound();
                }

                var response = _mapper.Map<BookDTO>(book);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return InternalError(ex);
            }
        }

        /// <summary>
        /// Creates a Book
        /// </summary>
        /// <param name="book"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] BookCreateDTO bookDTO)
        {
            try
            {
                if (bookDTO == null)
                {
                    _logger.LogWarn("Empty Book was submitted.");
                    return BadRequest(ModelState);
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarn("Book data was incomplete.");
                    return BadRequest(ModelState);
                }

                var book = _mapper.Map<Book>(bookDTO);
                var isSuccess = await _bookRepository.Create(book);
                if (!isSuccess)
                    return InternalError("Book creation failed.");

                return Created("Create", new { book });
            }
            catch (Exception ex)
            {
                return InternalError(ex);
            }
        }

        /// <summary>
        /// Updates a Book
        /// </summary>
        /// <param name="id"></param>
        /// <param name="book"></param>
        /// <returns></returns>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(int id, [FromBody] BookUpdateDTO bookDTO)
        {
            try
            {
                if (id < 1 || bookDTO == null || id != bookDTO.Id)
                {
                    _logger.LogWarn("Empty Book was submitted or id is invalid.");
                    return BadRequest(ModelState);
                }

                var isExists = await _bookRepository.IsExists(id);
                if (!isExists)
                {
                    _logger.LogWarn("Book was not found.");
                    return NotFound();
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarn("Book data was incomplete.");
                    return BadRequest(ModelState);
                }

                var book = _mapper.Map<Book>(bookDTO);
                var isSuccess = await _bookRepository.Update(book);
                if (!isSuccess)
                {
                    return InternalError("Book update failed.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return InternalError(ex);
            }
        }

        /// <summary>
        /// Deletes a Book
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id:int}")]
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
                    _logger.LogWarn("Invalid Book Id.");
                    return BadRequest();
                }

                var isExists = await _bookRepository.IsExists(id);
                if (!isExists)
                {
                    _logger.LogWarn("Book was not found.");
                    return NotFound();
                }

                var author = await _bookRepository.FindById(id);
                var isSuccess = await _bookRepository.Delete(author);
                if (!isSuccess)
                {
                    return InternalError("Book delete failed");
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
