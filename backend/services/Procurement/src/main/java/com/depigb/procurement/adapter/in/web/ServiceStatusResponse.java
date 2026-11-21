package com.depigb.procurement.adapter.in.web;

public record ServiceStatusResponse(
        String service,
        String status,
        String version,
        String environment) {
}
