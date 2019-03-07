package guts.client.models;

public class Result {

    private boolean success;
    private String message;

    public Result(boolean success) {
        this.success = success;
        this.message = "";
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
