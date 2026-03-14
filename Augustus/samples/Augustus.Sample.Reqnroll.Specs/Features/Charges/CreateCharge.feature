Feature: Create Charge
  As an API consumer
  I want to create a Stripe charge
  So that I can process payments

  Scenario: Successfully create a charge
    Given the Stripe API simulator is running
    When I create a charge for 2000 USD with source "tok_visa"
    Then the response should be successful
    And the response should contain a charge object

  Scenario: Charge returns correct currency
    Given the Stripe API simulator is running
    When I create a charge for 5000 EUR with source "tok_mastercard"
    Then the response should be successful
    And the charge currency should be "eur"
