# Rate Limiting Test Document 1

## Purpose
This document is part of a batch upload test to verify the RabbitMQ queue management and rate limiting functionality.

## Test Scenario
- **Batch Size**: 5 documents uploaded simultaneously
- **Expected Behavior**: Token bucket algorithm should manage the rate
- **Rate Limits**: 10 req/s document ingestion, 5 req/s embedding, 20 req/s search

## Technical Details
- **Queue Management**: RabbitMQ with priority queues
- **Circuit Breaker**: Should prevent cascade failures
- **Exponential Backoff**: Should handle temporary failures
- **Dead Letter Queue**: Should capture failed messages

## Document Metadata
- **Document Number**: 1 of 5
- **Upload Time**: Batch upload test
- **Expected Status**: Processing → Ready
- **Priority**: Normal

This is test document #1 for rate limiting verification.
