using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Farol_Seguro.Migrations
{
    /// <inheritdoc />
    public partial class SeedNiveisIniciais : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Cargos",
                columns: table => new
                {
                    Id_Cargo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome_Cargo = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cargos", x => x.Id_Cargo);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Estados",
                columns: table => new
                {
                    Id_Estado = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome_Estado = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Sigla_Estado = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Estados", x => x.Id_Estado);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Niveis",
                columns: table => new
                {
                    Id_Nivel = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome_Nivel = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Niveis", x => x.Id_Nivel);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Testemunhas",
                columns: table => new
                {
                    Id_Testemunha = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome_Testemunha = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telefone_Testemunha = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Testemunhas", x => x.Id_Testemunha);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Cidades",
                columns: table => new
                {
                    Id_Cidade = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome_Cidade = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Id_Estado = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cidades", x => x.Id_Cidade);
                    table.ForeignKey(
                        name: "FK_Cidades_Estados_Id_Estado",
                        column: x => x.Id_Estado,
                        principalTable: "Estados",
                        principalColumn: "Id_Estado",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Alunos",
                columns: table => new
                {
                    Id_Aluno = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome_Aluno = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email_Aluno = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Senha_Aluno = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Genero_Aluno = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataNascimento_Aluno = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Id_Nivel = table.Column<int>(type: "int", nullable: false),
                    ContadorDenunciasFalsas = table.Column<int>(type: "int", nullable: false),
                    IsBloqueado = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alunos", x => x.Id_Aluno);
                    table.ForeignKey(
                        name: "FK_Alunos_Niveis_Id_Nivel",
                        column: x => x.Id_Nivel,
                        principalTable: "Niveis",
                        principalColumn: "Id_Nivel",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Funcionarios",
                columns: table => new
                {
                    Id_Funcionario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome_Funcionario = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email_Funcionario = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Senha_Funcionario = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Departamento_Funcionario = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Id_Cargo = table.Column<int>(type: "int", nullable: false),
                    Id_Nivel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Funcionarios", x => x.Id_Funcionario);
                    table.ForeignKey(
                        name: "FK_Funcionarios_Cargos_Id_Cargo",
                        column: x => x.Id_Cargo,
                        principalTable: "Cargos",
                        principalColumn: "Id_Cargo",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Funcionarios_Niveis_Id_Nivel",
                        column: x => x.Id_Nivel,
                        principalTable: "Niveis",
                        principalColumn: "Id_Nivel",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id_Usuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome_Usuario = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email_Usuario = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Senha_Usuario = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Id_Nivel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id_Usuario);
                    table.ForeignKey(
                        name: "FK_Usuarios_Niveis_Id_Nivel",
                        column: x => x.Id_Nivel,
                        principalTable: "Niveis",
                        principalColumn: "Id_Nivel",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Escolas",
                columns: table => new
                {
                    Id_Escola = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nome_Escola = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Cnpj_Escola = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Endereco_Escola = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Telefone_Escola = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Id_Cidade = table.Column<int>(type: "int", nullable: false),
                    Id_Funcionario = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Escolas", x => x.Id_Escola);
                    table.ForeignKey(
                        name: "FK_Escolas_Cidades_Id_Cidade",
                        column: x => x.Id_Cidade,
                        principalTable: "Cidades",
                        principalColumn: "Id_Cidade",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Escolas_Funcionarios_Id_Funcionario",
                        column: x => x.Id_Funcionario,
                        principalTable: "Funcionarios",
                        principalColumn: "Id_Funcionario",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Denuncias",
                columns: table => new
                {
                    Id_Denuncia = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Titulo_Denuncia = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descricao_Denuncia = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DataCriacao_Denuncia = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Categoria_Denuncia = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status_Denuncia = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsFalsa = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Id_Aluno = table.Column<int>(type: "int", nullable: true),
                    Id_Escola = table.Column<int>(type: "int", nullable: false),
                    DenunciaAnonima = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Denuncias", x => x.Id_Denuncia);
                    table.ForeignKey(
                        name: "FK_Denuncias_Alunos_Id_Aluno",
                        column: x => x.Id_Aluno,
                        principalTable: "Alunos",
                        principalColumn: "Id_Aluno",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Denuncias_Escolas_Id_Escola",
                        column: x => x.Id_Escola,
                        principalTable: "Escolas",
                        principalColumn: "Id_Escola",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Anexos",
                columns: table => new
                {
                    Id_Anexo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Tipo_Anexo = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Caminho_Anexo = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NomeOriginal_Anexo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Id_Denuncia = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anexos", x => x.Id_Anexo);
                    table.ForeignKey(
                        name: "FK_Anexos_Denuncias_Id_Denuncia",
                        column: x => x.Id_Denuncia,
                        principalTable: "Denuncias",
                        principalColumn: "Id_Denuncia",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DenunciaTestemunhas",
                columns: table => new
                {
                    Id_Testemunha = table.Column<int>(type: "int", nullable: false),
                    Id_Denuncia = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DenunciaTestemunhas", x => new { x.Id_Denuncia, x.Id_Testemunha });
                    table.ForeignKey(
                        name: "FK_DenunciaTestemunhas_Denuncias_Id_Denuncia",
                        column: x => x.Id_Denuncia,
                        principalTable: "Denuncias",
                        principalColumn: "Id_Denuncia",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DenunciaTestemunhas_Testemunhas_Id_Testemunha",
                        column: x => x.Id_Testemunha,
                        principalTable: "Testemunhas",
                        principalColumn: "Id_Testemunha",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LogStatus",
                columns: table => new
                {
                    Id_Log = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Id_Denuncia = table.Column<int>(type: "int", nullable: false),
                    Status_Anterior = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status_Novo = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Id_Nivel = table.Column<int>(type: "int", nullable: false),
                    Nome_Nivel = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogStatus", x => x.Id_Log);
                    table.ForeignKey(
                        name: "FK_LogStatus_Denuncias_Id_Denuncia",
                        column: x => x.Id_Denuncia,
                        principalTable: "Denuncias",
                        principalColumn: "Id_Denuncia",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LogStatus_Niveis_Id_Nivel",
                        column: x => x.Id_Nivel,
                        principalTable: "Niveis",
                        principalColumn: "Id_Nivel",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Notificacao",
                columns: table => new
                {
                    Id_Notificacao = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Mensagem = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Lida = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UrlDestino = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Id_Denuncia = table.Column<int>(type: "int", nullable: false),
                    Id_Aluno = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificacao", x => x.Id_Notificacao);
                    table.ForeignKey(
                        name: "FK_Notificacao_Alunos_Id_Aluno",
                        column: x => x.Id_Aluno,
                        principalTable: "Alunos",
                        principalColumn: "Id_Aluno",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notificacao_Denuncias_Id_Denuncia",
                        column: x => x.Id_Denuncia,
                        principalTable: "Denuncias",
                        principalColumn: "Id_Denuncia",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Respostas",
                columns: table => new
                {
                    Id_Resposta = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Descricao_Resposta = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Data_Resposta = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Id_Denuncia = table.Column<int>(type: "int", nullable: false),
                    Id_Funcionario = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Respostas", x => x.Id_Resposta);
                    table.ForeignKey(
                        name: "FK_Respostas_Denuncias_Id_Denuncia",
                        column: x => x.Id_Denuncia,
                        principalTable: "Denuncias",
                        principalColumn: "Id_Denuncia",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Respostas_Funcionarios_Id_Funcionario",
                        column: x => x.Id_Funcionario,
                        principalTable: "Funcionarios",
                        principalColumn: "Id_Funcionario",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Niveis",
                columns: new[] { "Id_Nivel", "Nome_Nivel" },
                values: new object[,]
                {
                    { 1, "Aluno" },
                    { 2, "Funcionario" },
                    { 3, "Admin" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alunos_Id_Nivel",
                table: "Alunos",
                column: "Id_Nivel");

            migrationBuilder.CreateIndex(
                name: "IX_Anexos_Id_Denuncia",
                table: "Anexos",
                column: "Id_Denuncia");

            migrationBuilder.CreateIndex(
                name: "IX_Cidades_Id_Estado",
                table: "Cidades",
                column: "Id_Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Denuncias_Id_Aluno",
                table: "Denuncias",
                column: "Id_Aluno");

            migrationBuilder.CreateIndex(
                name: "IX_Denuncias_Id_Escola",
                table: "Denuncias",
                column: "Id_Escola");

            migrationBuilder.CreateIndex(
                name: "IX_DenunciaTestemunhas_Id_Testemunha",
                table: "DenunciaTestemunhas",
                column: "Id_Testemunha");

            migrationBuilder.CreateIndex(
                name: "IX_Escolas_Id_Cidade",
                table: "Escolas",
                column: "Id_Cidade");

            migrationBuilder.CreateIndex(
                name: "IX_Escolas_Id_Funcionario",
                table: "Escolas",
                column: "Id_Funcionario");

            migrationBuilder.CreateIndex(
                name: "IX_Funcionarios_Id_Cargo",
                table: "Funcionarios",
                column: "Id_Cargo");

            migrationBuilder.CreateIndex(
                name: "IX_Funcionarios_Id_Nivel",
                table: "Funcionarios",
                column: "Id_Nivel");

            migrationBuilder.CreateIndex(
                name: "IX_LogStatus_Id_Denuncia",
                table: "LogStatus",
                column: "Id_Denuncia");

            migrationBuilder.CreateIndex(
                name: "IX_LogStatus_Id_Nivel",
                table: "LogStatus",
                column: "Id_Nivel");

            migrationBuilder.CreateIndex(
                name: "IX_Notificacao_Id_Aluno",
                table: "Notificacao",
                column: "Id_Aluno");

            migrationBuilder.CreateIndex(
                name: "IX_Notificacao_Id_Denuncia",
                table: "Notificacao",
                column: "Id_Denuncia");

            migrationBuilder.CreateIndex(
                name: "IX_Respostas_Id_Denuncia",
                table: "Respostas",
                column: "Id_Denuncia");

            migrationBuilder.CreateIndex(
                name: "IX_Respostas_Id_Funcionario",
                table: "Respostas",
                column: "Id_Funcionario");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Id_Nivel",
                table: "Usuarios",
                column: "Id_Nivel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Anexos");

            migrationBuilder.DropTable(
                name: "DenunciaTestemunhas");

            migrationBuilder.DropTable(
                name: "LogStatus");

            migrationBuilder.DropTable(
                name: "Notificacao");

            migrationBuilder.DropTable(
                name: "Respostas");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Testemunhas");

            migrationBuilder.DropTable(
                name: "Denuncias");

            migrationBuilder.DropTable(
                name: "Alunos");

            migrationBuilder.DropTable(
                name: "Escolas");

            migrationBuilder.DropTable(
                name: "Cidades");

            migrationBuilder.DropTable(
                name: "Funcionarios");

            migrationBuilder.DropTable(
                name: "Estados");

            migrationBuilder.DropTable(
                name: "Cargos");

            migrationBuilder.DropTable(
                name: "Niveis");
        }
    }
}
