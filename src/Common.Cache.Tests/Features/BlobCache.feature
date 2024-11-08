@blob @integration
Feature: BlobCache

As a developer, I want to use blob for distributed cache

Background:
  Given blob storage is running

Scenario: able to store and retrieve item using blob cache
	Given a product
  | Id | Name | Price |
  | A1  | A    | 100   |
	When I store product in blob cache with key "A1"
  Then I should be able to retrieve product from blob cache with key "A1"
