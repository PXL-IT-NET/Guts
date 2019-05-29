package be.pxl.guts.core.http;

public class ApiResult {

    private int statusCode;
    private boolean success;
    private String message;

    public ApiResult(int statusCode, boolean success) {
        this(statusCode, success, "");
    }

    public ApiResult(int statusCode, boolean success, String message) {
        this.statusCode = statusCode;
        this.success = success;
        this.message = message;
    }

    public int getStatusCode() {
        return statusCode;
    }

    public boolean isSuccessful() {
        return success;
    }

    public String getMessage() {
        return message;
    }

    public void setMessage(String message) {
        this.message = message;
    }
}
