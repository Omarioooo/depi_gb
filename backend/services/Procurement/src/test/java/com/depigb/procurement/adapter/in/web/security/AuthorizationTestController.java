package com.depigb.procurement.adapter.in.web.security;

import jakarta.validation.Valid;
import jakarta.validation.constraints.NotBlank;
import java.util.Map;
import java.util.Objects;
import java.util.stream.Collectors;
import org.slf4j.MDC;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestAttribute;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

/**
 * Test-only controller that exercises every operation in the role matrix.
 *
 * <p>Production endpoints arrive in later sprints; this fixture proves the
 * filter, rules, and error handler work together without inventing API surface
 * that belongs to PROC-004 or later.</p>
 */
@RestController
@RequestMapping("/test/procurement")
public class AuthorizationTestController {

    private final AuthorizationRules rules;

    public AuthorizationTestController(AuthorizationRules rules) {
        this.rules = rules;
    }

    @GetMapping("/orders")
    Map<String, Object> readOrders(
            @RequestAttribute(UserContext.REQUEST_ATTRIBUTE) UserContext context) {
        return authorize(context, AuthorizationRules.Operation.READ_ORDERS);
    }

    @GetMapping("/receipts")
    Map<String, Object> readReceipts(
            @RequestAttribute(UserContext.REQUEST_ATTRIBUTE) UserContext context) {
        return authorize(context, AuthorizationRules.Operation.READ_RECEIPTS);
    }

    @PostMapping("/drafts")
    Map<String, Object> createDraft(
            @RequestAttribute(UserContext.REQUEST_ATTRIBUTE) UserContext context) {
        return authorize(context, AuthorizationRules.Operation.CREATE_DRAFT);
    }

    @PutMapping("/drafts/{id}")
    Map<String, Object> editDraft(
            @PathVariable String id,
            @RequestAttribute(UserContext.REQUEST_ATTRIBUTE) UserContext context) {
        return authorize(context, AuthorizationRules.Operation.EDIT_DRAFT);
    }

    @PostMapping("/orders/{id}/submit")
    Map<String, Object> submitOrder(
            @PathVariable String id,
            @RequestAttribute(UserContext.REQUEST_ATTRIBUTE) UserContext context) {
        return authorize(context, AuthorizationRules.Operation.SUBMIT_ORDER);
    }

    @PostMapping("/orders/{id}/approve")
    Map<String, Object> approveOrder(
            @PathVariable String id,
            @RequestAttribute(UserContext.REQUEST_ATTRIBUTE) UserContext context) {
        return authorize(context, AuthorizationRules.Operation.APPROVE_ORDER);
    }

    @PostMapping("/orders/{id}/cancel")
    Map<String, Object> cancelOrder(
            @PathVariable String id,
            @RequestAttribute(UserContext.REQUEST_ATTRIBUTE) UserContext context) {
        return authorize(context, AuthorizationRules.Operation.CANCEL_ORDER);
    }

    @PostMapping("/orders/{id}/receipts")
    Map<String, Object> recordReceipt(
            @PathVariable String id,
            @RequestAttribute(UserContext.REQUEST_ATTRIBUTE) UserContext context) {
        return authorize(context, AuthorizationRules.Operation.RECORD_RECEIPT);
    }

    @PostMapping("/receipts/{id}/retry")
    Map<String, Object> retryReceipt(
            @PathVariable String id,
            @RequestAttribute(UserContext.REQUEST_ATTRIBUTE) UserContext context) {
        return authorize(context, AuthorizationRules.Operation.RETRY_RECEIPT);
    }

    @PostMapping("/validate")
    Map<String, Object> validate(
            @Valid @RequestBody TestRequest request,
            @RequestAttribute(UserContext.REQUEST_ATTRIBUTE) UserContext context) {
        rules.require(context, AuthorizationRules.Operation.CREATE_DRAFT);
        return Map.of("name", request.name());
    }

    @GetMapping("/fail")
    Map<String, Object> fail(
            @RequestAttribute(UserContext.REQUEST_ATTRIBUTE) UserContext context) {
        rules.require(context, AuthorizationRules.Operation.READ_ORDERS);
        throw new IllegalStateException("internal detail that must not leak");
    }

    public record TestRequest(@NotBlank String name) {
    }

    private Map<String, Object> authorize(
            UserContext context, AuthorizationRules.Operation operation) {
        rules.require(context, operation);
        return Map.of(
                "userId", context.userId(),
                "roles", context.roles().stream()
                        .map(Enum::name)
                        .sorted()
                        .collect(Collectors.toList()),
                "correlationId", context.correlationId(),
                "mdcCorrelationId", Objects.requireNonNullElse(
                        MDC.get(GatewayHeaderFilter.CORRELATION_ID_MDC_KEY), ""));
    }
}
