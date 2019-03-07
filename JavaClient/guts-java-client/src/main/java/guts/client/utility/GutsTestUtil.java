package guts.client.utility;

import guts.client.annotations.GutsFixture;
import org.junit.platform.engine.TestSource;
import org.junit.platform.engine.support.descriptor.ClassSource;
import org.junit.platform.launcher.TestIdentifier;
import guts.client.GutsJUnit5;

import java.util.Optional;

public final class GutsTestUtil {

    public static ClassSource getGutsClassSource(TestIdentifier testIdentifier) {
        Optional<TestSource> testSourceOptional = testIdentifier.getSource();

        if(testSourceOptional.isPresent() && testSourceOptional.get() instanceof  ClassSource) {
            ClassSource classSource = (ClassSource) testSourceOptional.get();
            if(isGutsClassSource(classSource))
                return classSource;

            return null;
        }

        Optional<TestIdentifier> parentIdentifier = GutsJUnit5.getTestPlan().getParent(testIdentifier);
        return parentIdentifier.map(GutsTestUtil::getGutsClassSource).orElse(null);

    }

    public static boolean isGutsClassSource(ClassSource classSource) {
        return getGutsFixture(classSource) != null;
    }

    public static GutsFixture getGutsFixture(ClassSource classSource) {
        return classSource.getJavaClass().getAnnotation(GutsFixture.class);
    }

}
