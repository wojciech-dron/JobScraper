namespace JobScraper.Web.Features.AiSummary;

public class SummarizeOfferRequest
{
    public string? UserRequirements { get; set; } = "- project should be long term";

    public string? CvContent { get; set; } =
        """
        # .NET Developer

        ## Profil

        Programista .NET z 6-letnim doświadczeniem w budowaniu systemów analitycznych i rozproszonych. Specjalista w zakresie projektowania architektury, optymalizacji wydajności oraz wdrażania procesów CI/CD. Skupiony na dostarczaniu wartości biznesowej poprzez pragmatyczne rozwiązania technologiczne. Pasjonat technologii self-hosted.

        ---

        ## Umiejętności

        **Backend:** .NET 8/10, ASP.NET Core, C#, Entity Framework Core, Dapper, MediatR (CQRS), RabbitMQ, OpenTelemetry, Semantic Kernel (AI/LLM)

        **Frontend:** Angular 17+, TypeScript, Vue.js, ASP.NET Web Forms, Blazor, HTML, CSS

        **Bazy Danych:** Microsoft SQL Server, PostgreSQL, Redis, SQLite, Elasticsearch

        **DevOps & Narzędzia:** Azure DevOps, Docker, Kubernetes, AWS, CI/CD, GitLab, .NET Aspire, IIS, Git

        **Testowanie:** xUnit, NSubstitute, FluentAssertions, Playwright (E2E), nUnit

        **Metodologie:** DDD (Domain-Driven Design), Vertical Slice Architecture (VSA), SOLID, Saga Pattern, REST API, Camunda

        ---

        ## Doświadczenie Zawodowe

        ### Fullstack .NET Developer | Primaris Services
        **10.2023 – obecnie**

        - Projektowanie i rozwój systemów rozliczeniowych dla sektora paliwowego oraz aplikacji leasingowych dla bankowości w zespołach 6–10 osobowych.
        - **Osiągnięcie:** Skrócenie czasu wykonywania testów integracyjnych o 800% poprzez optymalizację strategii zarządzania bazą danych w kontenerach testowych.
        - Implementacja wzorców Saga i Outbox Pattern dla zapewnienia spójności transakcyjnej w rozproszonych modułach.
        - Automatyzacja rurociągów CI/CD, umożliwiająca bezpieczne wdrożenia multi-environment.
        - Konfiguracja pełnego stosu obserwowalności z użyciem OpenTelemetry, co zredukowało czas diagnozowania błędów.

        ---

        ### Fullstack .NET Developer | Uptime Development
        **07.2022 – 01.2024**

        - Budowa systemów zarządzania odpadami zintegrowanych z platformami rządowymi (BDO).
        - Samodzielne dostarczenie aplikacji do analizy technicznej rynków finansowych (akcje/krypto).
        - **Osiągnięcie:** Zarządzanie potokami CI/CD w Azure DevOps, redukując czas wdrażania o 40%.
        - Redukcja czasu wdrażania nowych pracowników o 30% poprzez aplikację Vertical Slice Architecture oraz zasad SOLID.
        - Tworzenie wewnętrznych bibliotek NuGet i szablonów projektowych, co przyśpieszyło tworzenie o ok. 50%.

        ---

        ### Fullstack .NET Developer | Ideo
        **03.2021 – 06.2022**

        - R&D głównej platformy Elektronicznego Obiegu Dokumentów (EOD) dla klientów korporacyjnych (>10 deweloperów).
        - Dostarczanie, utrzymywanie oraz rozwój dedykowanych wersji platformy dla 4 klientów.
        - **Osiągnięcie:** 40% wzrost wydajności dzięki optymalizacji procedur składowanych i indeksów w Microsoft SQL Server.
        - **Osiągnięcie:** Poprawa synchronizacji repozytoriów dla dedykowanych serwisów, redukując konflikty o 50%.

        ---

        ### .NET Developer | Tituto
        **12.2019 – 03.2021**

        - Budowa globalnej platformy job board dla branży medycznej (>7 deweloperów).
        - Dostarczenie aplikacji do zarządzania reklamami (>3 deweloperów).
        - Utrzymanie i rozwój stron internetowych dla instytucji publicznych (>3 deweloperów).
        - Refaktoryzacja architektury aplikacji, redukując złożoność o 30%.

        ---

        ### Stażysta (IoT Developer) | Rectangle
        **08.2019 – 11.2019**

        - Tworzenie oprogramowania (Linux, Qt, QML, GitLab) na urządzenia IoT oraz montaż prototypów.

        ---

        ## Edukacja

        **Politechnika Rzeszowska im. Ignacego Łukasiewicza**
        2016 – 2020 | mgr inż. Elektronika i Telekomunikacja

        ---

        ## Języki i Zainteresowania

        **Angielski:** C1 (Zaawansowany)

        **Zainteresowania:** LLM-y (Ollama, LM Studio), homelab (Home Assistant, n8n, Portainer), gry wideo
        """;

    public string? OfferContent { get; set; } =
        """
        Who We AreWe are a remote-first software as a service (SaaS) company, bringing true digital transformation to the global shipping industry. We enhance the way shipping professionals work by creating technology for the maritime industry and bringing it to market.With over 85% of the world’s trade transported by sea, we have a huge opportunity to transform existing manual, offline and disparate processes into a tech-enabled and data-rich experience enabling better decision-making and fewer costly and time-consuming mistakes. Our premier platform, Sea, is the world’s first digital shipping platform that provides cloud-based applications focused on the pre-fixture and at-fixture space. These connect to create efficiencies and digitise workflows. We are The Intelligent Marketplace for Fixing Freight. To understand more about us, please visit https://www.sea.live/The RoleWe are looking for a Lead Backend Engineer / Senior II, experienced in varied backend languages and frameworks to work on a platform revolutionising how maritime industry professionals; track their vessels, match their cargoes to vessels, manage scheduling activities, are involved in trading and negotiations and digital contract management. Working with us you will become part of an international team developing digital products for the maritime industry.Responsibilities
        Playing an active role in development and being hands-on alongside other engineers in your team building-out our cutting-edge products
        Setting and shaping what good practice is across the backend ecosystem, leading technical discussions and decisions from the front and employing the right tooling and services to do the job
        Architectural ownership within the team and contribution to the overall technical direction of the platform
        Directly engaged as part of the embedded product team to help shape the platform utilising direct client feedback and industry knowledge, all backed by data-driven evidence
        Leading your team to develop, test and release new functions and features
        Responsible for unit testing your code as well as conducting peer reviews,
        Working in the Scrum framework
        Using Azure DevOps for backlog and repository management
        Effective collaboration across a multi-disciplined team, interacting with Product, Architecture, DevOps functions of the business
        Work in an international team- the team consists of people of different nationalities, so all communication is based on English.
        Requirements
        Great understanding of English, both spoken and written. (B2+)

        Flexibility - We are a growing team working in a dynamic environment that requires engineers to be able to adapt quickly to change.

        Responsibility and Ownership – As a lead backend developer in our team we need you to have responsibility for the quality of your application(s). Owning your contributions and that of your technical team based on your guidance and technical leadership is vital.

        Commitment and proactivity - We need a person who is able to act and shows initiative. Cares about the development of the product.

        Openness and willingness to cooperate - Our team members work strongly together, are open to help, and are not afraid to ask questions. We need someone who fits into this kind of supportive and collaborative work style.
        Core Skills:
        Advanced knowledge and proficiency with .NET frameworks, ecosystem and tooling
        Mastery of working with SQL databases, proficiency with database design, performance querying and tuning, optimizations
        Knowledge of NoSQL technologies, especially CosmosDB, Redis
        Extensive experience in API design and build, both internal and externally-facing
        Deep knowledge of Security aspects of all the tech stack and how best to implement these in modern development
        Experience with message queueing systems
        Deep knowledge of distributed and scalable application development, microservices and containerization knowledge and the orchestration tools used
        Extensive Azure Cloud knowledge and DevOps exposure
        Hands-on experience with building SaaS systems
        Mentoring and coaching skills and experience
        Strong communication and problem solving skills
        What you can expectDespite our dynamic growth, we managed to maintain a relaxed and enjoyable atmosphere of a tightly knit team that can implement complex projects comprehensively and effectively. Each of us knows what is expected from us and has adequate space and freedom of action. The actual work is important, but it’s also essential for us at Sea that we all stay happy, relaxed and motivated. That’s why we provide a wide range of benefits to all our employees:
        Private medical care (Luxmed)
        Voluntary group life insurance
        MyBenefit or Multisport card
        Language courses (English and German)
        Mentoring program and numerous internal pieces of training
        Employee referral program
        Paid days off from services (B2B)
        A paid day off to care for your health - “Dzień na U”
        Integration events, joint company trips, birthday celebrations and many other
        What we offer
        Salary: DOE 28 000 - 32 000 PLN/month (B2B) or UoP
        Flexible working hours
        You choose how you work - from our office in Poznań or remotely from home, or like most of us, work hybrid
        Strong focus on growth, interesting projects & people who enjoy working with each other!


        Tech stackEnglishB2RedisadvancedMS AzureadvancedSoftware ArchitectureadvancedSaaSadvanced.NetadvancedLeadershipadvancedMentoringadvancedAzure DevOpsadvanced

        Office location
        PoznańWojskowa 6WarszawaWrocławPoznańGdańsk
        Published: 27.11.2025
        """;
}
