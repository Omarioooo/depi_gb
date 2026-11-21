package com.depigb.procurement;

import static org.assertj.core.api.Assertions.assertThat;

import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.web.client.TestRestTemplate;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;

@SpringBootTest(webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
class ProcurementApplicationTests {

    @Autowired
    private TestRestTemplate restTemplate;

    @Test
    void applicationStartsWithHealthEndpointUp() {
        ResponseEntity<String> response = restTemplate.getForEntity("/actuator/health", String.class);

        assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
        assertThat(response.getBody()).contains("\"status\":\"UP\"");
    }

    @Test
    void exposesLivenessAndReadinessProbes() {
        assertThat(restTemplate.getForEntity("/actuator/health/liveness", String.class))
                .extracting(ResponseEntity::getStatusCode)
                .isEqualTo(HttpStatus.OK);

        assertThat(restTemplate.getForEntity("/actuator/health/readiness", String.class))
                .extracting(ResponseEntity::getStatusCode)
                .isEqualTo(HttpStatus.OK);
    }

    @Test
    void exposesConfiguredServiceStatus() {
        ResponseEntity<String> response = restTemplate.getForEntity("/api/procurement/status", String.class);

        assertThat(response.getStatusCode()).isEqualTo(HttpStatus.OK);
        assertThat(response.getBody())
                .contains("\"service\":\"procurement-service\"")
                .contains("\"status\":\"UP\"");
    }
}
