# 📘 Documentation Technique — API E-Commerce (Backend)

## Projet Académique — ONS G4 TP4

---

## Table des matières

1. [Présentation du projet](#1-présentation-du-projet)
2. [Technologies utilisées](#2-technologies-utilisées)
3. [Architecture du projet](#3-architecture-du-projet)
4. [Structure des dossiers](#4-structure-des-dossiers)
5. [Base de données — Modèle de données](#5-base-de-données--modèle-de-données)
6. [Design Patterns utilisés](#6-design-patterns-utilisés)
7. [Authentification & Autorisation (JWT)](#7-authentification--autorisation-jwt)
8. [Système de rôles](#8-système-de-rôles)
9. [Couche Repository — Détails](#9-couche-repository--détails)
10. [Couche Services](#10-couche-services)
11. [Couche Controllers (API REST)](#11-couche-controllers-api-rest)
12. [DTOs (Data Transfer Objects)](#12-dtos-data-transfer-objects)
13. [Upload d'images](#13-upload-dimages)
14. [Système de promotions](#14-système-de-promotions)
15. [Configuration et démarrage](#15-configuration-et-démarrage)
16. [Endpoints API — Référence complète](#16-endpoints-api--référence-complète)
17. [Diagramme de flux](#17-diagramme-de-flux)

---

## 1. Présentation du projet

Ce projet est une **API REST E-Commerce** développée avec **ASP.NET Core 9** dans le cadre d'un projet académique. Elle permet la gestion complète d'une plateforme e-commerce multi-sociétés avec :

- Gestion des **sociétés** (Companies) par un SuperAdmin
- Gestion des **produits** et **catégories** par les administrateurs de chaque société
- Système de **commandes** pour les utilisateurs
- Système de **promotions** avec remises dynamiques
- **Authentification JWT** avec hachage de mot de passe from scratch (HMACSHA512)
- **Upload d'images** pour produits et catégories

---

## 2. Technologies utilisées

| Technologie | Version | Rôle |
|---|---|---|
| .NET | 9.0 | Framework principal |
| ASP.NET Core Web API | 9.0 | Framework REST API |
| Entity Framework Core | 9.0.11 | ORM (Object-Relational Mapping) |
| SQL Server (LocalDB) | — | Base de données relationnelle |
| JWT Bearer | 9.0.5 | Authentification par token |
| Swashbuckle (Swagger) | 9.0.6 | Documentation interactive de l'API |
| HMACSHA512 | — | Hachage de mot de passe (from scratch) |

---

## 3. Architecture du projet

L'architecture suit le modèle **N-Tier (multi-couches)** avec le **Repository Pattern** et le **Unit of Work Pattern** :

```
┌─────────────────────────────────────────────┐
│              CLIENT (Frontend)              │
│         (Angular / React / Postman)         │
└──────────────────┬──────────────────────────┘
                   │ HTTP Requests (JSON)
                   ▼
┌─────────────────────────────────────────────┐
│           CONTROLLERS (API REST)            │
│  AuthController, ProductsController, etc.   │
│  → Reçoit les requêtes HTTP                 │
│  → Valide les données (ModelState)          │
│  → Retourne les réponses (DTOs)             │
└──────────────────┬──────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────┐
│              SERVICES                       │
│  AuthService, ImageService                  │
│  → Logique métier complexe                  │
│  → Hachage des mots de passe               │
│  → Génération des tokens JWT               │
└──────────────────┬──────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────┐
│         UNIT OF WORK + REPOSITORIES         │
│  IUnitOfWork → UnitOfWork                   │
│  IProductRepository → ProductRepository     │
│  IOrderRepository → OrderRepository         │
│  IBaseRepository<T> → BaseRepository<T>     │
│  → Accès aux données                        │
│  → Transactions atomiques                   │
└──────────────────┬──────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────┐
│            DbContext (EF Core)              │
│         OltpDbContext                        │
│  → Mapping entités ↔ tables SQL             │
│  → Configurations Fluent API                │
└──────────────────┬──────────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────────┐
│          BASE DE DONNÉES SQL SERVER         │
│        G4_ECommerceDB (LocalDB)             │
└─────────────────────────────────────────────┘
```

### Pourquoi cette architecture ?

- **Séparation des responsabilités** : Chaque couche a un rôle précis
- **Testabilité** : Les interfaces permettent le mocking pour les tests unitaires
- **Maintenabilité** : Modifier une couche n'impacte pas les autres
- **Réutilisabilité** : Le `BaseRepository<T>` est générique pour toutes les entités

---

## 4. Structure des dossiers

```
API/
├── Controllers/              ← Contrôleurs API REST
│   ├── AuthController.cs
│   ├── CategoriesController.cs
│   ├── CompaniesController.cs
│   ├── OrdersController.cs
│   ├── ProductsController.cs
│   └── PromotionsController.cs
│
├── Data/                     ← DbContext (accès base de données)
│   └── OltpDbContext.cs
│
├── DTOs/                     ← Objets de transfert de données
│   ├── Auth/
│   │   ├── RegisterDto.cs
│   │   ├── LoginDto.cs
│   │   └── AuthResponseDto.cs
│   ├── Categories/
│   │   ├── CategoryDto.cs
│   │   ├── CreateCategoryDto.cs
│   │   └── UpdateCategoryDto.cs
│   ├── Companies/
│   │   ├── CompanyDto.cs
│   │   ├── CreateCompanyDto.cs
│   │   └── UpdateCompanyDto.cs
│   ├── Orders/
│   │   ├── OrderDto.cs
│   │   └── CreateOrderDto.cs
│   ├── Products/
│   │   ├── ProductDto.cs
│   │   ├── CreateProductDto.cs
│   │   └── UpdateProductDto.cs
│   └── Promotions/
│       ├── PromotionDto.cs
│       ├── CreatePromotionDto.cs
│       ├── UpdatePromotionDto.cs
│       └── AssignProductsDto.cs
│
├── Entities/
│   └── Oltp/                 ← Entités (modèles de données)
│       ├── Category.cs
│       ├── Company.cs
│       ├── Order.cs
│       ├── OrderItem.cs
│       ├── Product.cs
│       ├── Promotion.cs
│       └── User.cs
│
├── Migrations/
│   └── Oltp/                 ← Fichiers de migration EF Core
│
├── Repositories/
│   ├── Interfaces/           ← Contrats (interfaces)
│   │   ├── IBaseRepository.cs
│   │   ├── IProductRepository.cs
│   │   ├── IOrderRepository.cs
│   │   └── IUnitOfWork.cs
│   └── Implementations/     ← Implémentations concrètes
│       ├── BaseRepository.cs
│       ├── ProductRepository.cs
│       ├── OrderRepository.cs
│       └── UnitOfWork.cs
│
├── Services/                 ← Services métier
│   ├── IAuthService.cs
│   ├── AuthService.cs
│   ├── ImageService.cs       (contient aussi IImageService)
│   └── DbInitializer.cs
│
├── wwwroot/
│   └── assets/images/        ← Stockage des images uploadées
│       ├── products/
│       └── categories/
│
├── Program.cs                ← Point d'entrée + configuration DI
├── appsettings.json          ← Configuration (connexion DB, JWT)
└── API.csproj                ← Fichier projet (.NET)
```

---

## 5. Base de données — Modèle de données

### Diagramme relationnel

```
┌──────────────┐       ┌──────────────┐       ┌──────────────┐
│   Company    │       │   Category   │       │  Promotion   │
├──────────────┤       ├──────────────┤       ├──────────────┤
│ CompanyId PK │◄──┐   │ CategoryId PK│       │ PromotionId  │
│ Name         │   │   │ Name         │       │ Name         │
│ Description  │   ├──►│ CompanyId FK │       │ Discount %   │
│ Address      │   │   │ Description  │       │ StartDate    │
│ City         │   │   │ ImageUrl     │       │ EndDate      │
│ PhoneNumber  │   │   └──────┬───────┘       │ IsActive     │
│ Email        │   │          │               └──────┬───────┘
│ LogoUrl      │   │          │ 1:N                  │
│ IsActive     │   │          ▼                      │
│ CreatedAt    │   │   ┌──────────────┐              │
└──────────────┘   │   │   Product    │              │
       ▲           │   ├──────────────┤              │
       │           ├──►│ CompanyId FK │              │
       │           │   │ CategoryId FK│◄─────────────┘
       │           │   │ PromotionId FK (nullable)   │
       │ 1:N       │   │ Name         │              │
       │           │   │ Price        │              │
┌──────┴───────┐   │   │ StockQuantity│              │
│    User      │   │   │ ImageUrl     │              │
├──────────────┤   │   │ CreatedAt    │              │
│ UserId    PK │   │   └──────┬───────┘              │
│ FirstName    │   │          │                      │
│ LastName     │   │          │ 1:N                  │
│ Email        │   │          ▼                      │
│ PasswordHash │   │   ┌──────────────┐              │
│ PasswordSalt │   │   │  OrderItem   │              │
│ Role         │   │   ├──────────────┤              │
│ CompanyId FK │◄──┘   │ OrderItemId  │              │
│ PhoneNumber  │       │ ProductId FK │              │
│ Address      │       │ OrderId   FK │              │
│ City         │       │ Quantity     │              │
│ IsActive     │       │ UnitPrice    │              │
└──────┬───────┘       │ Subtotal     │              │
       │               └──────┬───────┘              │
       │ 1:N                  │                      │
       ▼                      │                      │
┌──────────────┐              │                      │
│    Order     │◄─────────────┘                      │
├──────────────┤                                     │
│ OrderId   PK │                                     │
│ UserId    FK │                                     │
│ OrderDate    │                                     │
│ TotalAmount  │                                     │
│ Status       │                                     │
│ ShippingAddr │                                     │
└──────────────┘                                     │
```

### Description des entités

| Entité | Description | Clés étrangères |
|--------|-------------|-----------------|
| **Company** | Société/entreprise sur la plateforme | — |
| **User** | Utilisateur (SuperAdmin, Admin, User) | CompanyId → Company (nullable) |
| **Category** | Catégorie de produits | CompanyId → Company |
| **Product** | Produit en vente | CategoryId → Category, CompanyId → Company, PromotionId → Promotion (nullable) |
| **Promotion** | Remise applicable aux produits | — |
| **Order** | Commande d'un utilisateur | UserId → User |
| **OrderItem** | Ligne de commande (produit + quantité) | OrderId → Order, ProductId → Product |

### Règles de suppression (Delete Behavior)

| Relation | Comportement | Raison |
|----------|-------------|--------|
| Product → Category | Restrict | Ne pas supprimer une catégorie qui a des produits |
| Product → Company | Restrict | Ne pas supprimer une société qui a des produits |
| Category → Company | Restrict | Ne pas supprimer une société qui a des catégories |
| User → Company | SetNull | Si société supprimée, l'utilisateur reste sans société |
| Order → User | Restrict | Ne pas supprimer un utilisateur qui a des commandes |
| OrderItem → Order | Cascade | Supprimer les lignes si la commande est supprimée |
| OrderItem → Product | Restrict | Ne pas supprimer un produit commandé |
| Product → Promotion | SetNull | Si promotion supprimée, le produit perd sa promo |

---

## 6. Design Patterns utilisés

### 6.1 Repository Pattern

**Objectif** : Abstraire l'accès aux données pour découpler les contrôleurs de Entity Framework.

```
Interface                    Implémentation
──────────                   ──────────────
IBaseRepository<T>    ──►    BaseRepository<T>
  ├── GetAllAsync()            └── Utilise OltpDbContext
  ├── GetByIdAsync(id)             via DbSet<T>
  ├── FindAsync(predicate)
  ├── AddAsync(entity)
  ├── Update(entity)
  └── Delete(entity)

IProductRepository    ──►    ProductRepository : BaseRepository<Product>
  ├── GetProductsWithCategoryAsync()     (Include Category, Promotion, Company)
  ├── GetProductWithCategoryAsync(id)
  ├── GetProductsByCategoryAsync(catId)
  └── SearchProductsAsync(keyword)

IOrderRepository      ──►    OrderRepository : BaseRepository<Order>
  ├── GetOrdersWithDetailsAsync()     (Include OrderItems→Product, User)
  ├── GetOrderWithDetailsAsync(id)
  └── GetOrdersByUserAsync(userId)
```

**Avantage** : Le `BaseRepository<T>` est **générique** — il fournit les opérations CRUD pour toutes les entités sans duplication de code.

### 6.2 Unit of Work Pattern

**Objectif** : Coordonner les opérations de plusieurs repositories dans une seule transaction.

```csharp
public interface IUnitOfWork : IDisposable
{
    IProductRepository Products { get; }
    IOrderRepository Orders { get; }
    IBaseRepository<Category> Categories { get; }
    IBaseRepository<OrderItem> OrderItems { get; }
    IBaseRepository<User> Users { get; }
    IBaseRepository<Promotion> Promotions { get; }
    IBaseRepository<Company> Companies { get; }
    Task<int> SaveChangesAsync();
}
```

**Utilisation dans un Controller** :
```csharp
// Une seule injection au lieu de 4 repositories séparés
private readonly IUnitOfWork _unitOfWork;

// Utiliser plusieurs repos dans une même transaction
var product = await _unitOfWork.Products.GetByIdAsync(id);
product.StockQuantity -= quantity;
_unitOfWork.Products.Update(product);
await _unitOfWork.Orders.AddAsync(order);
await _unitOfWork.SaveChangesAsync(); // Commit atomique
```

### 6.3 DTO Pattern (Data Transfer Object)

**Objectif** : Ne jamais exposer les entités directement au client. Les DTOs contrôlent exactement quelles données entrent et sortent de l'API.

```
Requête Client (JSON)           DTO d'entrée              Entité              DTO de sortie
─────────────────────           ──────────                ──────              ─────────────
{                         →    CreateProductDto      →    Product       →    ProductDto
  "name": "iPhone",            (validations)              (en BDD)           (avec champs calculés :
  "price": 999,                                                               DiscountedPrice,
  "categoryId": 1                                                              CategoryName,
}                                                                              CompanyName)
```

---

## 7. Authentification & Autorisation (JWT)

### Flux d'authentification

```
1. POST /api/auth/register  →  Créer un compte
   ┌──────────────────────────────────┐
   │ Body: { firstName, lastName,     │
   │         email, password, ... }   │
   └──────────────┬───────────────────┘
                  │
                  ▼
   ┌──────────────────────────────────┐
   │ AuthService.RegisterAsync()      │
   │ 1. Vérifier email unique         │
   │ 2. HMACSHA512 → hash + salt     │
   │ 3. Sauvegarder User en BDD      │
   │ 4. Générer token JWT             │
   └──────────────┬───────────────────┘
                  │
                  ▼
   ┌──────────────────────────────────┐
   │ Réponse: { token, userId,       │
   │   fullName, email, role,        │
   │   expiration }                   │
   └──────────────────────────────────┘

2. POST /api/auth/login  →  Se connecter
   ┌──────────────────────────────────┐
   │ Body: { email, password }        │
   └──────────────┬───────────────────┘
                  │
                  ▼
   ┌──────────────────────────────────┐
   │ AuthService.LoginAsync()         │
   │ 1. Trouver user par email        │
   │ 2. HMACSHA512(password, salt)    │
   │    → comparer avec hash stocké   │
   │ 3. Si OK → Générer token JWT     │
   └──────────────────────────────────┘

3. Requêtes authentifiées
   ┌──────────────────────────────────┐
   │ Header: Authorization:           │
   │   Bearer eyJhbGci...             │
   └──────────────┬───────────────────┘
                  │
                  ▼
   ┌──────────────────────────────────┐
   │ Middleware JWT valide le token   │
   │ → Vérifie signature, expiration │
   │ → Extrait les claims (UserId,   │
   │   Role, CompanyId)              │
   └──────────────────────────────────┘
```

### Hachage du mot de passe (From Scratch — HMACSHA512)

```csharp
// Création du hash
using var hmac = new HMACSHA512();           // Génère une clé aléatoire (salt)
passwordSalt = Convert.ToBase64String(hmac.Key);
passwordHash = Convert.ToBase64String(
    hmac.ComputeHash(Encoding.UTF8.GetBytes(password))
);

// Vérification
var saltBytes = Convert.FromBase64String(storedSalt);
using var hmac = new HMACSHA512(saltBytes);  // Réutilise le salt original
var computedHash = Convert.ToBase64String(
    hmac.ComputeHash(Encoding.UTF8.GetBytes(password))
);
return computedHash == storedHash;           // Comparaison
```

### Structure du token JWT

Le token contient les **claims** suivants :
- `NameIdentifier` : UserId
- `Name` : Nom complet de l'utilisateur
- `Email` : Email
- `Role` : SuperAdmin / Admin / User
- `CompanyId` : ID de la société (si applicable)
- `Jti` : Identifiant unique du token
- Expiration : 60 minutes (configurable)

---

## 8. Système de rôles

| Rôle | Description | Permissions |
|------|-------------|-------------|
| **SuperAdmin** | Administrateur global de la plateforme | Créer/modifier/supprimer des sociétés, voir toutes les commandes |
| **Admin** | Administrateur d'une société | CRUD produits/catégories/promotions de sa société |
| **User** | Client final | Passer des commandes, voir ses propres commandes |

### Matrice des permissions par endpoint

| Endpoint | SuperAdmin | Admin | User | Anonyme |
|----------|:----------:|:-----:|:----:|:-------:|
| POST /api/auth/register | — | — | — | ✅ |
| POST /api/auth/login | — | — | — | ✅ |
| GET /api/companies | ✅ | ❌ | ❌ | ❌ |
| POST /api/companies | ✅ | ❌ | ❌ | ❌ |
| GET /api/products | ✅ | ✅ | ✅ | ✅ |
| POST /api/products | ❌ | ✅ | ❌ | ❌ |
| PUT /api/products/{id} | ❌ | ✅* | ❌ | ❌ |
| GET /api/categories | ✅ | ✅ | ✅ | ✅ |
| POST /api/categories | ❌ | ✅ | ❌ | ❌ |
| PUT /api/categories/{id} | ❌ | ✅* | ❌ | ❌ |
| GET /api/orders | ✅ | ✅ | ❌ | ❌ |
| GET /api/orders/my-orders | ✅ | ✅ | ✅ | ❌ |
| POST /api/orders | ✅ | ✅ | ✅ | ❌ |
| GET /api/promotions | ✅ | ✅ | ✅ | ✅ |
| POST /api/promotions | ❌ | ✅ | ❌ | ❌ |

*\* Uniquement pour les ressources de sa propre société*

---

## 9. Couche Repository — Détails

### IBaseRepository<T> — Opérations génériques

```csharp
public interface IBaseRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    Task<int> SaveChangesAsync();
}
```

Ce repository générique est **réutilisé par toutes les entités**. Les entités sans logique spéciale (Category, User, Company, Promotion, OrderItem) utilisent directement `IBaseRepository<T>` via le UnitOfWork.

### IProductRepository — Opérations spécialisées

Hérite de `IBaseRepository<Product>` et ajoute des requêtes avec `Include()` pour charger les relations :
- `GetProductsWithCategoryAsync()` → Include Category + Promotion + Company
- `SearchProductsAsync(keyword)` → Recherche par nom ou description

### IOrderRepository — Opérations spécialisées

Hérite de `IBaseRepository<Order>` et ajoute :
- `GetOrdersWithDetailsAsync()` → Include OrderItems → Product, User
- `GetOrdersByUserAsync(userId)` → Commandes d'un utilisateur spécifique

---

## 10. Couche Services

### AuthService

| Méthode | Description |
|---------|-------------|
| `RegisterAsync(RegisterDto)` | Inscrit un nouvel utilisateur, hache le mot de passe, génère un JWT |
| `LoginAsync(LoginDto)` | Vérifie les identifiants, génère un JWT si valides |
| `GenerateJwtToken(User)` | Crée un token JWT avec les claims de l'utilisateur |
| `CreatePasswordHash(...)` | Hache un mot de passe avec HMACSHA512 (from scratch) |
| `VerifyPasswordHash(...)` | Vérifie un mot de passe contre un hash + salt stockés |

### ImageService

| Méthode | Description |
|---------|-------------|
| `SaveImageAsync(IFormFile, subFolder)` | Sauvegarde une image dans `wwwroot/assets/images/{subFolder}/` avec un nom unique (GUID) |
| `DeleteImage(imageUrl)` | Supprime physiquement le fichier image du serveur |

### DbInitializer

| Méthode | Description |
|---------|-------------|
| `InitializeAsync(IServiceProvider)` | Applique les migrations automatiquement et crée un SuperAdmin par défaut (superadmin@ecommerce.com / SuperAdmin@123) si la base est vide |

---

## 11. Couche Controllers (API REST)

Chaque controller suit le pattern REST standard :

```
GET    /api/{resource}        → Liste toutes les ressources
GET    /api/{resource}/{id}   → Récupère une ressource par ID
POST   /api/{resource}        → Crée une nouvelle ressource
PUT    /api/{resource}/{id}   → Met à jour une ressource
DELETE /api/{resource}/{id}   → Supprime une ressource
```

### Flux d'une requête POST (création de produit)

```
1. Client envoie POST /api/products (avec [FromForm] + image)
   │
2. ProductsController.Create() reçoit CreateProductDto
   │  → Vérifie ModelState.IsValid
   │  → Vérifie que l'Admin a un CompanyId
   │
3. ImageService.SaveImageAsync(dto.Image, "products")
   │  → Sauvegarde le fichier dans wwwroot/assets/images/products/
   │  → Retourne le chemin relatif URL
   │
4. Crée l'entité Product avec les données du DTO
   │
5. _unitOfWork.Products.AddAsync(product)
   │  → EF Core ajoute au ChangeTracker
   │
6. _unitOfWork.SaveChangesAsync()
   │  → INSERT INTO Products VALUES(...)
   │
7. Récupère le produit créé avec ses relations (Include)
   │
8. Retourne 201 Created avec ProductDto (mappage entité → DTO)
```

---

## 12. DTOs (Data Transfer Objects)

### Pourquoi des DTOs ?

1. **Sécurité** : Ne jamais exposer PasswordHash/PasswordSalt au client
2. **Contrôle** : Choisir exactement les champs envoyés/reçus
3. **Validation** : Annotations `[Required]`, `[StringLength]`, `[Range]` sur les DTOs d'entrée
4. **Données calculées** : `DiscountedPrice`, `ProductCount`, `FullName` dans les DTOs de sortie

### Exemples

```csharp
// DTO d'entrée — avec validations
public class CreateProductDto
{
    [Required] [StringLength(200)]
    public string Name { get; set; }

    [Required] [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }

    public IFormFile? Image { get; set; }   // Upload fichier
    [Required] public int CategoryId { get; set; }
}

// DTO de sortie — avec champs calculés
public class ProductDto
{
    public int ProductId { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string CategoryName { get; set; }      // ← Nom de la catégorie (jointure)
    public string CompanyName { get; set; }        // ← Nom de la société (jointure)
    public decimal? DiscountedPrice { get; set; }  // ← Prix après remise (calculé)
}
```

---

## 13. Upload d'images

### Fonctionnement

1. Le client envoie la requête en **multipart/form-data** (pas JSON)
2. Le controller utilise `[FromForm]` au lieu de `[FromBody]`
3. Le DTO contient une propriété `IFormFile? Image`
4. `ImageService.SaveImageAsync()` :
   - Génère un nom unique (GUID + extension originale)
   - Crée le dossier cible si il n'existe pas
   - Écrit le fichier dans `wwwroot/assets/images/{subFolder}/`
   - Retourne le chemin URL relatif `/assets/images/products/abc123.jpg`
5. L'URL est stockée dans la colonne `ImageUrl` de l'entité

### Accès aux images

Les images sont servies comme fichiers statiques grâce à `app.UseStaticFiles()` :
```
GET https://localhost:5001/assets/images/products/abc123.jpg
```

---

## 14. Système de promotions

### Modèle

- Une **Promotion** a un pourcentage de remise, une date de début et une date de fin
- Un **Produit** peut être lié à **une seule** promotion (relation 1:N Promotion → Products)
- L'Admin peut **affecter** ou **retirer** des produits d'une promotion

### Calcul du prix remisé

```csharp
// Dans le mapping ProductDto
var hasPromo = product.Promotion != null
    && product.Promotion.IsActive
    && product.Promotion.StartDate <= DateTime.UtcNow
    && product.Promotion.EndDate >= DateTime.UtcNow;

DiscountedPrice = hasPromo
    ? product.Price - (product.Price * product.Promotion.DiscountPercentage / 100)
    : null;
```

Le prix remisé est **calculé dynamiquement** et n'est jamais stocké en base de données.

---

## 15. Configuration et démarrage

### appsettings.json

```json
{
  "ConnectionStrings": {
    "OltpConnection": "Server=(localdb)\\MSSQLLocalDB;Database=G4_ECommerceDB;..."
  },
  "JwtSettings": {
    "SecretKey": "VotreCléSecrète...",
    "Issuer": "API-ECommerce",
    "Audience": "API-ECommerce-Client",
    "ExpirationInMinutes": 60
  }
}
```

### Injection de dépendances (Program.cs)

```csharp
// DbContext
builder.Services.AddDbContext<OltpDbContext>(...);

// Repository Pattern
builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IImageService, ImageService>();
```

### Commandes pour exécuter le projet

```bash
# Restaurer les packages NuGet
dotnet restore

# Appliquer les migrations (créer la base de données)
dotnet ef database update --context OltpDbContext

# Lancer l'API
dotnet run

# L'API est accessible sur : https://localhost:5001
# Swagger UI : https://localhost:5001/swagger
```

### SuperAdmin par défaut

Au premier lancement, `DbInitializer` crée automatiquement :
- **Email** : `superadmin@ecommerce.com`
- **Mot de passe** : `SuperAdmin@123`
- **Rôle** : SuperAdmin

---

## 16. Endpoints API — Référence complète

### Auth
| Méthode | Endpoint | Auth | Description |
|---------|----------|------|-------------|
| POST | `/api/auth/register` | ❌ | Inscription (rôle User uniquement) |
| POST | `/api/auth/login` | ❌ | Connexion → retourne un token JWT |

### Companies (SuperAdmin uniquement)
| Méthode | Endpoint | Auth | Description |
|---------|----------|------|-------------|
| GET | `/api/companies` | SuperAdmin | Liste toutes les sociétés |
| GET | `/api/companies/{id}` | SuperAdmin | Détails d'une société |
| POST | `/api/companies` | SuperAdmin | Créer une société + son Admin |
| PUT | `/api/companies/{id}` | SuperAdmin | Modifier une société |
| DELETE | `/api/companies/{id}` | SuperAdmin | Supprimer une société |

### Categories
| Méthode | Endpoint | Auth | Description |
|---------|----------|------|-------------|
| GET | `/api/categories` | ❌ | Liste toutes les catégories |
| GET | `/api/categories/{id}` | ❌ | Détails d'une catégorie |
| POST | `/api/categories` | Admin | Créer une catégorie (FormData + image) |
| PUT | `/api/categories/{id}` | Admin | Modifier une catégorie |
| DELETE | `/api/categories/{id}` | Admin | Supprimer une catégorie |

### Products
| Méthode | Endpoint | Auth | Description |
|---------|----------|------|-------------|
| GET | `/api/products` | ❌ | Liste tous les produits |
| GET | `/api/products/{id}` | ❌ | Détails d'un produit |
| GET | `/api/products/category/{id}` | ❌ | Produits par catégorie |
| GET | `/api/products/search?keyword=xxx` | ❌ | Recherche par mot-clé |
| POST | `/api/products` | Admin | Créer un produit (FormData + image) |
| PUT | `/api/products/{id}` | Admin | Modifier un produit |
| DELETE | `/api/products/{id}` | Admin | Supprimer un produit |

### Orders
| Méthode | Endpoint | Auth | Description |
|---------|----------|------|-------------|
| GET | `/api/orders` | Admin/SuperAdmin | Toutes les commandes |
| GET | `/api/orders/{id}` | Connecté | Détails d'une commande |
| GET | `/api/orders/my-orders` | Connecté | Mes commandes |
| POST | `/api/orders` | Connecté | Passer une commande |

### Promotions
| Méthode | Endpoint | Auth | Description |
|---------|----------|------|-------------|
| GET | `/api/promotions` | ❌ | Liste toutes les promotions |
| GET | `/api/promotions/{id}` | ❌ | Détails d'une promotion |
| GET | `/api/promotions/{id}/products` | ❌ | Produits d'une promotion |
| POST | `/api/promotions` | Admin | Créer une promotion |
| PUT | `/api/promotions/{id}` | Admin | Modifier une promotion |
| DELETE | `/api/promotions/{id}` | Admin | Supprimer une promotion |
| POST | `/api/promotions/{id}/assign-products` | Admin | Affecter des produits |
| POST | `/api/promotions/{id}/remove-products` | Admin | Retirer des produits |

---

## 17. Diagramme de flux

### Flux de création d'une commande

```
Client (User connecté)
    │
    ▼
POST /api/orders
    { shippingAddress: "...",
      items: [{ productId: 1, quantity: 2 }] }
    │
    ▼
OrdersController.Create()
    │
    ├── Extraire UserId du token JWT
    ├── Pour chaque item :
    │   ├── Vérifier que le produit existe
    │   ├── Vérifier le stock disponible
    │   ├── Calculer le sous-total (price × quantity)
    │   └── Décrémenter le stock du produit
    ├── Calculer le montant total
    ├── Créer l'entité Order + OrderItems
    │
    ▼
UnitOfWork.SaveChangesAsync()
    │
    ├── INSERT INTO Orders (...)
    ├── INSERT INTO OrderItems (...)
    └── UPDATE Products SET StockQuantity = ...
    │
    ▼
Réponse 201 Created → OrderDto
```

### Flux de création d'une société (SuperAdmin)

```
SuperAdmin
    │
    ▼
POST /api/companies
    { name: "Ma Société",
      adminFirstName: "Ahmed",
      adminEmail: "ahmed@societe.com",
      adminPassword: "***" }
    │
    ▼
CompaniesController.Create()
    │
    ├── Vérifier que l'email admin n'existe pas
    ├── Créer l'entité Company → SaveChanges
    ├── Hacher le mot de passe admin (HMACSHA512)
    ├── Créer l'entité User (rôle Admin, CompanyId)
    │
    ▼
UnitOfWork.SaveChangesAsync()
    │
    ├── INSERT INTO Companies (...)
    └── INSERT INTO Users (..., Role='Admin', CompanyId=X)
    │
    ▼
Réponse 201 Created → CompanyDto
```

---

*Document généré pour le projet académique ONS G4 TP4 — API E-Commerce Backend*
