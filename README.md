# DotDecentralized library

This is a work in progress implementation of [W3C decentralized identifier specification (DID)](https://www.w3.org/TR/did-core/). It is a .NET Standard 2.1 library based on [System.Text.Json](https://docs.microsoft.com/en-us/dotnet/api/system.text.json) and will be moved to .NET 5. The class hierarchy should useable in other serialization formats also.

## Guiding ideas

- A typed system that can operate on any decentralized identifier and verifiable credentials.

- Allow mixing and matching cryptograhic libraries in verification methods and relationships without recompiling the library and allowing choosing library preferably dynamically.

- Allow composing and inheriting specialized implementations using core classes while maintaining security, performance,
develope experience etc. The main is to build a bare core and layer these on top of the core (e.g. extension methods).

- Do not throw exception and stop processing if a strongly typed class isn
not available but collect extra parameters to [JsonExtensionData](https://docs.microsoft.com/en-us/dotnet/api/system.text.json.serialization.jsonextensiondataattribute)

- Implement specification specific rule checking as methods that operate on the typed system.

- Does not aim to implement JSON-LD processing rules (Expanded, N-Quads, Framed etc.).

- Implement the official test suite and a large number of tests using real data from other implementations.

- Add project files for VS Code.


## References

The following specifications and guidelines are referenced:
- [DID Implementation Guide v1.0: An implementation guide for DID software developers](https://w3c.github.io/did-imp-guide/)
- [Decentralized Identifiers (DIDs) v1.0: Core architecture, data model, and representations](https://www.w3.org/TR/did-core/)
- [DID Specification Registries: The interoperability registry for Decentralized Identifiers](https://www.w3.org/TR/did-spec-registries/)
- [Verifiable Credentials Data Model Implementation Report 1.0:Implementation Report for the Verifiable Credentials Data Model](https://w3c.github.io/vc-test-suite/implementations/)
- [Verifiable Credentials Data Model 1.0: Expressing verifiable information on the Web](https://www.w3.org/TR/vc-data-model/)

The aim is also to make possible or implement some other works listed in [Decentralized Identity Foundation (DIF)](https://identity.foundation/#wgs).