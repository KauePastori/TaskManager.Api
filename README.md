TaskManager PRO — .NET 8 • C# • EF Core • SQLite

Aplicação Web API para gestão de Projetos e Tarefas (relação 1:N),
construída em C# (.NET 8) com Entity Framework Core e SQLite. O projeto
demonstra modelagem de entidades, CRUD completo, relacionamentos,
filtros/ordenação/paginação, soft delete, validações, Swagger e seed de
dados.

testes: Postman incluído neste repositório.

-------------------------------------------------------------------------------
📌 Tema escolhido Task Manager PRO – Cadastre Projetos e gerencie suas
Tarefas com status (Todo, InProgress, Done), data de entrega e
anotações. O foco é aplicar conceitos de desenvolvimento de software em
C# com persistência via EF Core, incluindo relacionamentos, migrações
(opcional) e boas práticas.

O que a aplicação faz - CRUD de Projetos (Project). - CRUD de Tarefas
(TaskItem) vinculadas a um projeto (1:N). - Filtros avançados para
tarefas: por projectId, status, search (título/notas), orderBy e
paginação. - Soft delete de tarefas (não aparecem nas listagens após
remoção). - Cascade delete: ao apagar um projeto, suas tarefas são
removidas. - Swagger para explorar e testar rapidamente os endpoints.

-------------------------------------------------------------------------------

🧱 Arquitetura e organização TaskManager/ └─ TaskManager.Api/ ├─ Data/ │
└─ AppDbContext.cs ├─ DTOs/ │ ├─ ProjectDtos.cs │ └─ TaskDtos.cs ├─
Models/ │ ├─ Project.cs │ └─ TaskItem.cs ├─ Properties/ │ └─
launchSettings.json ├─ Program.cs ├─ appsettings.json └─
TaskManager.Api.csproj

Principais decisões técnicas - Minimal APIs (.NET 8): endpoints
objetivos e fáceis de ler. - DTOs de entrada: ProjectCreate/UpdateDto,
TaskCreate/UpdateDto com validação por atributos (\[Required\],
\[MaxLength\]). - Enum com strings no JSON: WorkStatus serializado por
nome (ex.: "InProgress") usando JsonStringEnumConverter. - Soft delete
em TaskItem com HasQueryFilter (não retorna itens apagados). - Audit
fields: CreatedAt / UpdatedAt atualizados automaticamente no
SaveChangesAsync. - Filtro/Ordenação/Paginação nos GETs. - Swagger
organizado por tags. - CORS liberado para desenvolvimento. - Criação de
schema garantida: se não houver migrações, EnsureCreatedAsync() + seed
manual.

-------------------------------------------------------------------------------
🧩 Modelo de dados (EF Core) Entidades - Project  - Id (int), Name
(string, obrigatório, 120), Description (string?, 500)  - CreatedAt,
UpdatedAt  - Tasks (lista de TaskItem) - TaskItem  - Id (int), Title
(string, obrigatório, 160), Notes (string?), DueDate (DateTime?)  -
Status (WorkStatus: Todo, InProgress, Done)  - IsDeleted (bool),
CreatedAt, UpdatedAt  - ProjectId (FK), Project (nav)

Relacionamento - 1:N — Project tem muitas TaskItem. - Cascade delete do
EF Core: apagar um Project apaga suas TaskItem.

-------------------------------------------------------------------------------
🛠️ Pré-requisitos - .NET 8 SDK: https://dotnet.microsoft.com/download -
(opcional) EF Core CLI (para usar migrações formalmente): dotnet tool
install --global dotnet-ef

-------------------------------------------------------------------------------
▶️ Como configurar e rodar Via CLI cd TaskManager/TaskManager.Api dotnet
restore dotnet run

\- API: http://localhost:5178 - Swagger: http://localhost:5178/swagger

Via Visual Studio 1) Abra a pasta TaskManager/TaskManager.Api. 2)
Selecione o perfil http (Debug). 3) Pressione F5. O navegador abrirá o
Swagger automaticamente.

Banco de dados (SQLite) - O arquivo taskmanager.db é criado
automaticamente na raiz do projeto API. - Sem migrações: o projeto
executa EnsureCreatedAsync() e aplica seed inicial (1 projeto + 2
tarefas). - Com migrações (recomendado em avaliação EF): cd
TaskManager/TaskManager.Api dotnet ef migrations add InitialCreate
dotnet ef database update dotnet run

Dica: se trocar entre EnsureCreated e Migrations, apague o arquivo
taskmanager.db para evitar conflito de schema.

-------------------------------------------------------------------------------
🔗 Documentação dos endpoints (Swagger) PROJECTS - GET
/api/projects?page=1&pageSize=20 - GET /api/projects/{id} - POST
/api/projects { "name": "Meu Projeto", "description": "Descrição" } -
PUT /api/projects/{id} { "name": "Meu Projeto (Atualizado)",
"description": "Com filtros e soft delete" } - DELETE /api/projects/{id}

TASKS - GET
/api/tasks?projectId=1&status=InProgress&search=&orderBy=dueDate&page=1&pageSize=20 -
GET /api/tasks/{id} - POST /api/tasks { "title": "Escrever README",
"notes": "Adicionar nomes do grupo e prints", "dueDate":
"2025-10-15T00:00:00Z", "status": "InProgress", "projectId": 1 } - PUT
/api/tasks/{id} { "title": "Escrever README (rev)", "notes": "Completar
critérios de avaliação", "dueDate": "2025-10-18T00:00:00Z", "status":
"Done", "projectId": 1 } - DELETE /api/tasks/{id} (soft delete)

Paginação (contrato) { "total": 42, "page": 1, "pageSize": 20, "items":
\[ ... \] }

-------------------------------------------------------------------------------
🧪 Testes rápidos (Swagger / Postman) 1) Criar Projeto → POST
/api/projects 2) Criar Tarefas ligadas ao projeto → POST /api/tasks 3)
Listar com filtros → GET
/api/tasks?projectId=1&status=InProgress&orderBy=dueDate 4) Atualizar →
PUT /api/tasks/{id} 5) Deletar (soft) → DELETE /api/tasks/{id} 6)
Deletar Projeto → DELETE /api/projects/{id}

Coleções incluídas no repo (pasta /collections): -
TaskManager_Postman\_\*.json - TaskManager_Insomnia\_\*.json

-------------------------------------------------------------------------------
🔒 Validações e erros - Validações por atributos (\[Required\],
\[MaxLength\]) nos DTOs. - Enum WorkStatus: aceito como string no JSON
(ex.: "Done"). - Erro padrão: ProblemDetails (RFC 7807) com type, title,
status, detail.

-------------------------------------------------------------------------------
🌱 Seed de dados Criado automaticamente quando o banco é gerado sem
migrações: - Project: "Challenge XPTO" - Tasks: "Configurar ambiente
(Todo)", "Criar endpoints CRUD (InProgress)"

-------------------------------------------------------------------------------
🧯 Troubleshooting (erros comuns) - CS0104 “TaskStatus é referência
ambígua” → resolvido renomeando o enum para WorkStatus. - CS1737
“Parâmetros opcionais…” → handlers ajustados: obrigatórios antes dos
opcionais. - Failed to read parameter ... as JSON → envie status como
string (ex.: "InProgress"); o projeto já inclui
JsonStringEnumConverter. - SQLite Error 1: 'no such table: Projects' →
apague taskmanager.db e rode novamente; ou use migrações (dotnet ef
...).

-------------------------------------------------------------------------------
👥 Integrantes do grupo
- Kauê Pastori Teixeira — RM98501
- Nicolas Nogueira Boni - RM551965
- Enzo Sartorelli - RM96618
- Eduardo Nistal - RM94524

-------------------------------------------------------------------------------
📄 Licença Projeto para o CP.
