package com.depigb.procurement.adapter.in.web;

import org.springframework.beans.factory.annotation.Value;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

/**
 * Lightweight service status endpoint used to verify the inbound web adapter
 * is wired and the runtime environment is configured.
 */
@RestController
@RequestMapping("/api/procurement")
public class ServiceStatusController {

    private final String serviceName;
    private final String version;
    private final String environment;

    public ServiceStatusController(
            @Value("${spring.application.name}") String serviceName,
            @Value("${info.app.version:unknown}") String version,
            @Value("${info.app.environment:unknown}") String environment) {
        this.serviceName = serviceName;
        this.version = version;
        this.environment = environment;
    }

    @GetMapping("/status")
    public ServiceStatusResponse status() {
        return new ServiceStatusResponse(serviceName, "UP", version, environment);
    }
}
