namespace Adaya.Orders.Application.Common;

public abstract class AppException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

// 404 => data ga nemu
public class NotFoundException(string resource, object key)
    : AppException("not_found", $"{resource} dengan id '{key}' tidak ditemukan.");

// 400 => bad request
public class RequestValidationException(IDictionary<string, string[]> errors)
    : AppException("validation_error", "Satu atau lebih field tidak valid.")
{
    public IDictionary<string, string[]> Errors { get; } = errors;
}

// 422 >> stock kosong
public class InsufficientStockException(int productId, int requested)
    : AppException("insufficient_stock", $"Stock produk {productId} tidak cukup untuk quantity {requested}.");

// 422 duplicate requestt
public class IdempotencyKeyReuseException()
    : AppException("idempotency_key_reuse", "Idempotency-Key sudah dipakai untuk request dengan isi yang berbeda.");

// 409 concurrency conflict
public class ConcurrencyConflictException()
    : AppException("concurrency_conflict", "Order sudah diubah oleh pihak lain. Muat ulang lalu coba lagi.");


public class DuplicateIdempotencyKeyException() : Exception("Idempotency key sudah ada.");