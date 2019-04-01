package be.pxl.guts.core;

import be.pxl.guts.core.auth.IAuthorizationHandler;
import be.pxl.guts.core.auth.swing.SwingAuthorizationHandler;
import be.pxl.guts.core.config.BasicConfig;
import be.pxl.guts.core.config.Configuration;
import be.pxl.guts.core.config.IConfig;
import be.pxl.guts.core.http.ApiResult;
import be.pxl.guts.core.http.GutsHttpClient;
import be.pxl.guts.core.http.IHttpClient;
import be.pxl.guts.core.http.TestResultSender;
import be.pxl.guts.core.logger.GutsLogger;
import be.pxl.guts.core.util.TestAccumulator;

public class GutsCore {

    private GutsLogger logger;
    private Configuration configuration;

    private IHttpClient httpClient;
    private IAuthorizationHandler authorizationHandler;
    private TestResultSender testResultSender;

    public GutsCore() {
        this(new BasicConfig());
    }

    public GutsCore(IConfig config) {
        this(config, null);
    }

    public GutsCore(IConfig config, IAuthorizationHandler authorizationHandler) {
        this.logger = new GutsLogger();
        this.configuration = new Configuration(config);

        this.httpClient = new GutsHttpClient(getConfig(), getLogger());
        this.authorizationHandler = authorizationHandler == null ? new SwingAuthorizationHandler(this.logger, this.httpClient, this.getConfig()) : authorizationHandler;
        this.testResultSender = new TestResultSender(getLogger(), getConfig(), this.httpClient, this.authorizationHandler);
    }

    public ApiResult sendResults(TestAccumulator testAccumulator) {
        return testResultSender.send(testAccumulator.toAssignmentTestRun(), testAccumulator.getTestRunType());
    }

    public GutsLogger getLogger() {
        return logger;
    }

    public IConfig getConfig() {
        return configuration.getConfig();
    }
}
