using Farol_Seguro.Models;
using Microsoft.EntityFrameworkCore;

namespace Farol_Seguro.Config
{
    public class DbConfig : DbContext
    {
        public DbConfig(DbContextOptions<DbConfig> options) : base(options) { }

        // DbSets de todas as tabelas
        public DbSet<Estado> Estados { get; set; }
        public DbSet<Cidade> Cidades { get; set; }
        public DbSet<Cargo> Cargos { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Funcionario> Funcionarios { get; set; }
        public DbSet<Escola> Escolas { get; set; }
        public DbSet<Aluno> Alunos { get; set; }
        public DbSet<Testemunha> Testemunhas { get; set; }
        public DbSet<Denuncia> Denuncias { get; set; }
        public DbSet<Anexo> Anexos { get; set; }
        public DbSet<Resposta> Respostas { get; set; }
        public DbSet<DenunciaTestemunha> DenunciaTestemunhas { get; set; }
        public DbSet<Nivel> Niveis { get; set; }
        public DbSet<Notificacao> Notificacao { get; set; }
        public DbSet<LogStatus> LogStatus { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Nivel>().HasData(
                new Nivel { Id_Nivel = 1, Nome_Nivel = "Aluno" },
                new Nivel { Id_Nivel = 2, Nome_Nivel = "Funcionario" },
                new Nivel { Id_Nivel = 3, Nome_Nivel = "Admin" }
            );

            // 🔹 Relacionamento N:N Denuncia ↔ Testemunha
            modelBuilder.Entity<DenunciaTestemunha>()
                .HasKey(dt => new { dt.Id_Denuncia, dt.Id_Testemunha });

            modelBuilder.Entity<DenunciaTestemunha>()
                .HasOne(dt => dt.Denuncias)
                .WithMany(d => d.DenunciaTestemunhas)
                .HasForeignKey(dt => dt.Id_Denuncia);

            modelBuilder.Entity<DenunciaTestemunha>()
                .HasOne(dt => dt.Testemunha)
                .WithMany(t => t.DenunciaTestemunhas)
                .HasForeignKey(dt => dt.Id_Testemunha);

            // 🔹 Estado -> Cidade (1:N)
            modelBuilder.Entity<Cidade>()
                .HasOne(c => c.Estado)
                .WithMany(e => e.Cidades)
                .HasForeignKey(c => c.Id_Estado);

            // 🔹 Cidade -> Escola (1:N)
            modelBuilder.Entity<Escola>()
                .HasOne(e => e.Cidade)
                .WithMany(c => c.Escolas)
                .HasForeignKey(e => e.Id_Cidade);

            // 🔹 Cargo -> Funcionarios (1:N)
            modelBuilder.Entity<Funcionario>()
                .HasOne(f => f.Cargo)
                .WithMany(c => c.Funcionarios)
                .HasForeignKey(f => f.Id_Cargo);

            // 🔹 Escola -> Denuncia (1:N)
            modelBuilder.Entity<Denuncia>()
                .HasOne(d => d.Escola)
                .WithMany(e => e.Denuncias)
                .HasForeignKey(d => d.Id_Escola);

            // 🔹 Aluno -> Denuncia (1:N)
            //modelBuilder.Entity<Denuncia>()
            //    .HasOne(d => d.Aluno)
            //    .WithMany(a => a.Denuncias)
            //    .HasForeignKey(d => d.Id_Aluno);
            modelBuilder.Entity<Denuncia>(entity =>
            {
                entity.HasOne(d => d.Aluno)
                .WithMany(a => a.Denuncias)
                .HasForeignKey(d => d.Id_Aluno)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
            });

            // 🔹 Denuncia -> Anexos (1:N)
            modelBuilder.Entity<Anexo>()
                .HasOne(a => a.Denuncia)
                .WithMany(d => d.Anexos)
                .HasForeignKey(a => a.Id_Denuncia);

            // 🔹 Denuncia -> Respostas (1:N)
            modelBuilder.Entity<Resposta>()
                .HasOne(r => r.Denuncia)
                .WithMany(d => d.Respostas)
                .HasForeignKey(r => r.Id_Denuncia);

            // 🔹 Usuario -> Nivel (N:1)
            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Nivel)
                .WithMany(n => n.Usuarios)
                .HasForeignKey(u => u.Id_Nivel);

            modelBuilder.Entity<Notificacao>()
                .HasOne(n => n.Aluno)
                .WithMany(a => a.Notificacoes) // Assumindo que você adicionou a coleção 'Notificacoes' na classe Aluno (veja o passo 3)
                .HasForeignKey(n => n.Id_Aluno)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notificacao>()
            .HasOne(n => n.Denuncia)
            .WithMany(d => d.Notificacoes) // Assumindo que você adicionou a coleção 'Notificacoes' na classe Denuncia (veja o passo 3)
            .HasForeignKey(n => n.Id_Denuncia)
            .OnDelete(DeleteBehavior.Restrict); // Recomendo Restrict ou SetNull, para
        }
    }
}
