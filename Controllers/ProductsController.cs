using System.Runtime.CompilerServices;
using ApiEcommerce.Models;
using ApiEcommerce.Models.Dtos;
using ApiEcommerce.Models.Dtos.Responses;
using ApiEcommerce.Repository.IRepository;
using Asp.Versioning;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiEcommerce.Controllers
{
  [Authorize(Roles = "Admin")]
  [Route("api/v{version:apiVersion}/[controller]")]
  [ApiVersionNeutral]

  [ApiController]
  public class ProductsController : ControllerBase
  {
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    public ProductsController(IProductRepository productRepository, ICategoryRepository categoryRepository)
    {
      _productRepository = productRepository;
      _categoryRepository = categoryRepository;
    }

    // ! Obtener todos los productos
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetProducts()
    {
      var products = _productRepository.GetProducts();
      var productsDto = products.Adapt<List<ProductDto>>();
      return Ok(productsDto);
    }
    // ! Obtener un producto por el Id
    [AllowAnonymous]
    [HttpGet("{productId:int}", Name = "GetProduct")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetProduct(int productId)
    {
      var product = _productRepository.GetProduct(productId);
      if (product == null)
      {
        return NotFound($"El producto con el id {productId} no existe");
      }
      var productDto = product.Adapt<ProductDto>();
      return Ok(productDto);
    }

    // ! Obtener los productos paginados

    [AllowAnonymous]
    [HttpGet("Paged", Name = "GetProductsInPage")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetProductsInPage([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5)
    {
      if (pageNumber < 1 || pageSize < 1)
      {
        return BadRequest("Los parámetros de paginación no son válidos");
      }
      var totalProducts = _productRepository.GetTotalProducts();
      var totalPages = Math.Ceiling((double)totalProducts / pageSize);
      if (pageNumber > totalPages)
      {
        return NotFound("No hay productos para mostrar en esta página");
      }
      var products = _productRepository.GetProductsInPages(pageNumber, pageSize);
      if (products == null || products.Count == 0)
      {
        return NotFound("No hay productos para mostrar");
      }

      var productDto = products.Adapt<List<ProductDto>>();
      var paginationResponse = new PaginationResponse<ProductDto>
      {
        PageNumber = pageNumber,
        PageSize = pageSize,
        TotalPages = (int)totalPages,
        Items = productDto
      };
      return Ok(paginationResponse);
    }

    // ! Crear un producto nuevo

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult CreateProduct([FromForm] CreateProductDto createProductDto)
    {
      if (createProductDto == null)
      {
        return BadRequest(ModelState);
      }
      if (_productRepository.ProductExists(createProductDto.Name))
      {
        ModelState.AddModelError("CustomError", "El producto ya existe");
        return BadRequest(ModelState);
      }
      if (!_categoryRepository.CategoryExists(createProductDto.CategoryId))
      {
        ModelState.AddModelError("CustomError", $"La categoría con el {createProductDto.CategoryId} no existe");
        return BadRequest(ModelState);
      }
      var product = createProductDto.Adapt<Product>();

      // * Agregar una imagen al producto

      UploadProductImage(createProductDto, product);

      // *

      if (!_productRepository.CreateProduct(product))
      {
        ModelState.AddModelError("CustomError", $"Algo salió mal al guardar el registro {product.Name}");
        return StatusCode(500, ModelState);
      }
      var createdProduct = _productRepository.GetProduct(product.ProductId);
      var productoDto = createdProduct.Adapt<ProductDto>();
      return CreatedAtRoute("GetProduct", new { productId = product.ProductId }, productoDto);
    }

    // ! Obtener un producto por la categoría

    [HttpGet("searchProductByCategory/{categoryId:int}", Name = "GetProductsForCategory")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetProductsForCategory(int categoryId)
    {
      var products = _productRepository.GetProductsForCategory(categoryId);
      if (products.Count == 0)
      {
        return NotFound($"Los productos con la categoría {categoryId} no existen");
      }
      var productsDto = products.Adapt<List<ProductDto>>();
      return Ok(productsDto);
    }

    // ! Obtener un producto por la descripción

    [HttpGet("searchProductByNameDescription/{searchTerm}", Name = "SearchProducts")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult SearchProducts(string searchTerm)
    {
      var products = _productRepository.SearchProducts(searchTerm);
      if (products.Count == 0)
      {
        return NotFound($"Los productos con el nombre o descripción '{searchTerm}' no existen");
      }
      var productsDto = products.Adapt<List<ProductDto>>();
      return Ok(productsDto);
    }

    // ! Gestionar la compra de un producto

    [HttpPatch("buyProduct/{name}/{quantity:int}", Name = "BuyProduct")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult BuyProduct(string name, int quantity)
    {
      if (string.IsNullOrWhiteSpace(name) || quantity <= 0)
      {
        return BadRequest("El nombre del producto o la cantidad no son válidos");
      }
      var foundProduct = _productRepository.ProductExists(name);
      if (!foundProduct)
      {
        return NotFound($"El producto con el nombre {name} no existe");
      }
      if (!_productRepository.BuyProduct(name, quantity))
      {
        ModelState.AddModelError("CustomError", $"No se pudo comprar el producto {name} o la cantidad solicitada es mayor al stock disponible");
        return BadRequest(ModelState);
      }
      var units = quantity == 1 ? "unidad" : "unidades";
      return Ok($"Se compro {quantity} {units} del producto '{name}'");
    }

    // ! Actualizar los datos de un producto

    [HttpPut("{productId:int}", Name = "UpdateProduct")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult UpdateProduct(int productId, [FromForm] UpdateProductDto updateProductDto)
    {
      if (updateProductDto == null)
      {
        return BadRequest(ModelState);
      }
      if (!_productRepository.ProductExists(productId))
      {
        ModelState.AddModelError("CustomError", "El producto no existe");
        return BadRequest(ModelState);
      }
      if (!_categoryRepository.CategoryExists(updateProductDto.CategoryId))
      {
        ModelState.AddModelError("CustomError", $"La categoría con el {updateProductDto.CategoryId} no existe");
        return BadRequest(ModelState);
      }
      var product = updateProductDto.Adapt<Product>();
      product.ProductId = productId;

      // * Agregar una imagen a un producto ya creado

      UploadProductImage(updateProductDto, product);

      // *


      if (!_productRepository.UpdateProduct(product))
      {
        ModelState.AddModelError("CustomError", $"Algo salió mal al actualizar el registro {product.Name}");
        return StatusCode(500, ModelState);
      }
      return NoContent();
    }

    private void UploadProductImage(dynamic productDto, Product product)
    {
      if (productDto.Image != null)
      {
        string fileName = product.ProductId + Guid.NewGuid().ToString() + Path.GetExtension(productDto.Image.FileName);
        var imagesFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ProductsImages");
        if (!Directory.Exists(imagesFolder))
        {
          Directory.CreateDirectory(imagesFolder);
        }
        var localPath = Path.Combine(imagesFolder, fileName);
        FileInfo file = new FileInfo(localPath);
        if (file.Exists)
        {
          file.Delete();
        }
        using (var stream = new FileStream(localPath, FileMode.Create))
        {
          productDto.Image.CopyTo(stream);
        }
        var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";
        product.ImgUrl = $"{baseUrl}/ProductsImages/{fileName}";
        product.ImgUrlLocal = localPath;

      }
      else
      {
        product.ImgUrl = "https://placehold.co/300x300";
      }
    }

    // ! Eliminar un producto de la base

    [HttpDelete("{productId:int}", Name = "DeleteProduct")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult DeleteProduct(int productId)
    {
      if (productId == 0)
      {
        return BadRequest(ModelState);
      }

      var product = _productRepository.GetProduct(productId);
      if (product == null)
      {
        return NotFound($"El producto con el id {productId} no existe");
      }
      if (!_productRepository.DeleteProduct(product))
      {
        ModelState.AddModelError("CustomError", $"Algo salió mal al eliminar el registro {product.Name}");
        return StatusCode(500, ModelState);
      }
      return NoContent();
    }
  }
}
