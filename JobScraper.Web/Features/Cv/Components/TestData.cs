using JobScraper.Web.Blazor.Components;
using JobScraper.Web.Common.Models;

namespace JobScraper.Web.Features.Cv.Components;

// TODO: remove after development
public static class TestData
{
    public static readonly byte[] Image = File.ReadAllBytes("Features/Cv/Components/image.jpg");

    #region BaseCv
    public const string BaseCv =
        """
        # Adam Kowalski - Senior .NET Developer

        **Telefon:** +48 608 228 288

        **Email:** adam.kowalski@example.com

        **Adres:** Rzeszów, Polska

        **LinkedIn:** https://www.linkedin.com/in/adam-kowalski-7227721a2/

        **GitHub:** https://github.com/adam-kowalski

        ## Profil

        Programista .NET z 6-letnim doświadczeniem w budowaniu systemów analitycznych i rozproszonych.
        Specjalista w zakresie projektowania architektury, optymalizacji wydajności oraz wdrażania procesów CI/CD.
        Skupiony na dostarczaniu wartości biznesowej poprzez pragmatyczne rozwiązania technologiczne.
        Pasjonat technologii self-hosted.

        ## Umiejętności

        **Backend:** .NET 8/10, ASP.NET Core, C#, Entity Framework Core, Dapper, MediatR (CQRS), RabbitMQ, OpenTelemetry, Semantic Kernel (AI/LLM)

        **Frontend:** Angular 17+, TypeScript, Vue.js, ASP.NET Web Forms, Blazor, HTML, CSS

        **Bazy Danych:** Microsoft SQL Server, PostgreSQL, Redis, SQLite, Elasticsearch

        **DevOps & Narzędzia:** Azure DevOps, Docker, Kubernetes, AWS, CI/CD, GitLab, .NET Aspire, IIS, Git

        **Testowanie:** xUnit, NSubstitute, FluentAssertions, Playwright (E2E), nUnit

        **Metodologie:** DDD (Domain-Driven Design), Vertical Slice Architecture (VSA), SOLID, Saga Pattern, REST API, Camunda

        ## Doświadczenie Zawodowe

        ### Fullstack .NET Developer | Software House

        **10.2023 – obecnie**

        - Projektowanie i rozwój systemów rozliczeniowych oraz aplikacji finansowych w zespołach 6–10 osobowych.
        - **Osiągnięcie:** Skrócenie czasu wykonywania testów integracyjnych o 800% poprzez optymalizację strategii zarządzania bazą danych w kontenerach testowych.
        - Implementacja wzorców Saga i Outbox Pattern dla zapewnienia spójności transakcyjnej w rozproszonych modułach.
        - Automatyzacja rurociągów CI/CD, umożliwiająca bezpieczne wdrożenia multi-environment.
        - Konfiguracja pełnego stosu obserwowalności z użyciem OpenTelemetry, co zredukowało czas diagnozowania błędów.

        ### Fullstack .NET Developer | Software Development Agency

        **07.2022 – 01.2024**

        - Budowa systemów zarządzania zasobami zintegrowanych z platformami zewnętrznymi.
        - Samodzielne dostarczenie aplikacji do analizy danych rynkowych.
        - **Osiągnięcie:** Zarządzanie potokami CI/CD w Azure DevOps, redukując czas wdrażania o 40%.
        - Redukcja czasu wdrażania nowych pracowników o 30% poprzez aplikację Vertical Slice Architecture oraz zasad SOLID.
        - Tworzenie wewnętrznych bibliotek NuGet i szablonów projektowych, co przyśpieszyło tworzenie o ok. 50%.

        ### Fullstack .NET Developer | E-commerce & EOD Solutions Provider

        **03.2021 – 06.2022**

        - R&D głównej platformy Elektronicznego Obiegu Dokumentów (EOD) dla klientów korporacyjnych (>10 deweloperów).
        - Dostarczanie, utrzymywanie oraz rozwój dedykowanych wersji platformy dla 4 klientów.
        - **Osiągnięcie:** 40% wzrost wydajności dzięki optymalizacji procedur składowanych i indeksów w Microsoft SQL Server.
        - **Osiągnięcie:** Poprawa synchronizacji repozytoriów dla dedykowanych serwisów, redukując konflikty o 50%.

        ### .NET Developer | HealthTech Startup

        **12.2019 – 03.2021**

        - Budowa globalnej platformy typu job board dla specjalistycznej branży (>7 deweloperów).
        - Dostarczenie aplikacji do zarządzania reklamami (>3 deweloperów).
        - Utrzymanie i rozwój stron internetowych dla sektora publicznego (>3 deweloperów).
        - Refaktoryzacja architektury aplikacji, redukując złożoność o 30%.

        ### Stażysta (IoT Developer) | IoT Solutions Company

        **08.2019 – 11.2019**

        - Tworzenie oprogramowania (Linux, Qt, QML, GitLab) na urządzenia IoT oraz montaż prototypów.

        ## Edukacja

        **Politechnika (Uczelnia Techniczna)**
        2016 – 2020 | mgr inż. Elektronika i Telekomunikacja

        ## Języki i Zainteresowania

        **Angielski:** C1 (Zaawansowany)

        **Zainteresowania:** LLM-y (Ollama, LM Studio), homelab (Home Assistant, n8n, Portainer), gry wideo

        """;
    #endregion

    #region ModifiedCv
    public const string ModifiedCv =
        """
        # Adam Kowalski - Senior .NET Developer

        **Telefon:** +48 608 228 288

        **Email:** adam.kowalski@example.com

        **Adres:** Rzeszów, Polska

        **LinkedIn:** https://www.linkedin.com/in/adam-kowalski-7227721a2/

        **GitHub:** https://github.com/adam-kowalski

        ## Profil

        Programista .NET z 6-letnim doświadczeniem w budowaniu systemów analitycznych i rozproszonych.
        Specjalista w zakresie projektowania architektury, optymalizacji wydajności oraz wdrażania procesów CI/CD.
        Skupiony na dostarczaniu wartości biznesowej poprzez pragmatyczne rozwiązania technologiczne.
        Pasjonat technologii self-hosted.

        ## Umiejętności

        **Backend:** .NET 8/10, ASP.NET Core, C#, Entity Framework Core, Dapper, MediatR (CQRS), RabbitMQ, OpenTelemetry, Semantic Kernel (AI/LLM)

        **Frontend:** Angular 17+, TypeScript, Vue.js, ASP.NET Web Forms, Blazor, HTML, CSS

        **Bazy Danych:** Microsoft SQL Server, PostgreSQL, Redis, SQLite, Elasticsearch

        **DevOps & Narzędzia:** Azure DevOps, Docker, Kubernetes, AWS, CI/CD, GitLab, .NET Aspire, IIS, Git

        **Testowanie:** xUnit, NSubstitute, FluentAssertions, Playwright (E2E), nUnit

        **Metodologie:** DDD (Domain-Driven Design), Vertical Slice Architecture (VSA), SOLID, Saga Pattern, REST API, Camunda

        ## Doświadczenie Zawodowe

        ### Fullstack .NET Developer | Software House

        **10.2023 – obecnie**

        - Projektowanie i rozwój systemów rozliczeniowych oraz aplikacji finansowych w zespołach 6–10 osobowych.
        - **Osiągnięcie:** Skrócenie czasu wykonywania testów integracyjnych o 800% poprzez optymalizację strategii zarządzania bazą danych w kontenerach testowych.
        - dodatkowa pozycja
        - Implementacja wzorców Saga i Outbox Pattern dla zapewnienia spójności transakcyjnej w rozproszonych modułach.
        - Automatyzacja rurociągów CI/CD, umożliwiająca bezpieczne wdrożenia multi-environment.

        ### Fullstack .NET Developer | Software Development Agency

        **07.2022 – 01.2024**

        - Budowa systemów zarządzania zasobami zintegrowanych z platformami zewnętrznymi.
        - Samodzielne dostarczenie aplikacji do analizy danych rynkowych.
        - **Osiągnięcie:** Zarządzanie potokami CI/CD w Azure DevOps, redukując czas wdrażania o 40%.
        - Redukcja czasu wdrażania nowych pracowników o 30% poprzez aplikację Vertical Slice Architecture oraz zasad SOLID.
        - Tworzenie wewnętrznych bibliotek NuGet i szablonów projektowych, co przyśpieszyło tworzenie o ok. 50%.

        ### Fullstack .NET Developer | E-commerce & EOD Solutions Provider

        **03.2021 – 06.2022**

        - R&D głównej platformy Elektronicznego Obiegu Dokumentów (EOD) dla klientów korporacyjnych (>10 deweloperów).
        - Dostarczanie, utrzymywanie oraz rozwój dedykowanych wersji platformy dla 4 klientów.
        - **Osiągnięcie:** 40% wzrost wydajności dzięki optymalizacji procedur składowanych i indeksów w Microsoft SQL Server.
        - **Osiągnięcie:** Poprawa synchronizacji repozytoriów dla dedykowanych serwisów, redukując konflikty o 50%.

        ### .NET Developer | HealthTech Startup

        **12.2019 – 03.2021**

        - Budowa globalnej platformy typu job board dla specjalistycznej branży (>7 deweloperów).
        - Dostarczenie aplikacji do zarządzania reklamami (>3 deweloperów).
        - Utrzymanie i rozwój stron internetowych dla sektora publicznego (>3 deweloperów).
        - Refaktoryzacja architektury aplikacji, redukując złożoność o 30%.

        ### Stażysta (IoT Developer) | IoT Solutions Company

        **08.2019 – 11.2019**

        - Tworzenie oprogramowania (Linux, Qt, QML, GitLab) na urządzenia IoT oraz montaż prototypów.

        ## Edukacja

        **Politechnika (Uczelnia Techniczna)**
        2016 – 2020 | mgr inż. Elektronika i Telekomunikacja

        ## Języki i Zainteresowania

        **Angielski:** C1 (Zaawansowany)

        **Zainteresowania:** LLM-y (Ollama, LM Studio), homelab (Home Assistant, n8n, Portainer), gry wideo

        """;
    #endregion

    #region ChatExample
    public static readonly List<ChatItem> Chat =
    [
        new ChatItem("assistant",
            "assistant",
            """
            ## Analysis of Job Offer vs. CV for Senior C# Engineer Position

            ### **Strong Matches (✅)**

            **Experience Level:**
            - 6 years .NET development (exceeds 5+ requirement)
            - Senior-level expertise demonstrated through complex achievements

            **Core Technical Skills:**
            - C#/.NET (primary requirement - fully matched)
            - Multi-threading (explicitly demonstrated in achievements)
            - Service-oriented architecture (CQRS, MediatR patterns)
            - SQL Server and PostgreSQL (direct match)
            - Redis (explicit match)
            - Git (explicit match)
            - Test-driven development (xUnit, nUnit, FluentAssertions)
            - Problem-solving (multiple achievements demonstrate this)
            - English C1 level (exceeds requirement)

            **Cloud & DevOps:**
            - AWS experience (listed)
            - Azure DevOps (explicit match)
            - Docker, Kubernetes (relevant for cloud deployment)

            ### **Partial Matches (⚠️)**

            **Messaging Systems:**
            - Has RabbitMQ experience (similar to Kafka/Solace)
            - Transferable knowledge of message queues and distributed systems

            **CI/CD:**
            - Azure DevOps experience (matches Jenkins partially)
            - GitLab experience (similar to Jenkins)

            ### **Missing Requirements (❌)**

            **Databases:**
            - Snowflake experience (not mentioned in CV)

            **Messaging Systems:**
            - Kafka and Solace experience (only RabbitMQ mentioned)

            **DevOps Tools:**
            - Jenkins (has Azure DevOps/GitLab instead)
            - JIRA (not mentioned)
            - Octopus Deploy (not mentioned)

            ### **ATS Optimization Suggestions**

            **Keywords to Highlight:**
            - "multi-threaded applications"
            - "service-oriented architecture"
            - "distributed systems"
            - "cloud platforms (AWS/Azure)"
            - "message queue systems"
            - "CI/CD pipelines"
            - "test-driven development"
            - "problem-solving"

            **Sections to Enhance:**
            1. **Skills Section:** Add Snowflake, Kafka, Solace as "familiar with" or "learning"
            2. **Experience Section:** Emphasize achievements involving:
               - Multi-threading optimization
               - Service-oriented design
               - Cloud deployments
               - Message queue implementations
            3. **Achievements:** Frame existing achievements using job posting keywords

            **Specific CV Edits:**
            - Add Snowflake to databases list with "familiar with"
            - Add Kafka/Solace to messaging systems with "learning/interested in"
            - Add JIRA to tools list with "familiar with"
            - Add Octopus Deploy with "interested in learning"
            - Rephrase achievements to include "multi-threaded," "service-oriented," "distributed systems"

            **Transferable Skills to Emphasize:**
            - RabbitMQ → Kafka/Solace (message queue patterns)
            - Azure DevOps → Jenkins (CI/CD concepts)
            - Entity Framework Core → Snowflake (database optimization)

            The CV is already very strong for this position, with 80-85% of requirements already met. The gaps are relatively minor and can be addressed through keyword optimization and framing existing experience appropriately.
            """),
    ];
    #endregion
}
