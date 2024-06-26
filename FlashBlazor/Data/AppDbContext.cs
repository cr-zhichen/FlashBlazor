﻿using Microsoft.EntityFrameworkCore;

namespace FlashBlazor;

/// <summary>
/// 数据库上下文
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="options"></param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// UserInfo 表
    /// </summary>
    public DbSet<UserInfo> TestUser { get; set; }

    /// <summary>
    /// 数据库配置
    /// </summary>
    /// <param name="optionsBuilder"></param>
    override protected void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
    }

    /// <summary>
    /// 配置模型
    /// </summary>
    /// <param name="modelBuilder"></param>
    override protected void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 配置 UserInfo 表的映射
        modelBuilder
            .Entity<UserInfo>(entity =>
            {
                entity.HasKey(e => e.UserId).HasName("PRIMARY");
                entity.Property(e => e.UserId).ValueGeneratedOnAdd();
                entity.HasIndex(u => u.Username).IsUnique();
                entity.Property(e => e.Role).HasConversion<string>();
            });
    }
}
